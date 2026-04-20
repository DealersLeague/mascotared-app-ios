using Mascotared.Services;

namespace Mascotared.Views;

public partial class Favoritos : ContentPage
{
    private const string MediaBaseUrl = "https://api.mascotared.es";
    private readonly ApiService _api = new();
    private readonly List<MomentoItem> _posts = new();
    private readonly string _miId = Preferences.Get("user_id", "");

    public Favoritos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Badge mensajes no leídos
        try
        {
            var conv = await _api.GetConversacionesAsync();
            bool hayNoLeidos = conv.Any(j =>
                j.TryGetProperty("noLeidos", out var nl) && nl.GetInt32() > 0);
            BadgeNoLeidos.IsVisible = hayNoLeidos;
            BadgeNoLeidos2.IsVisible = hayNoLeidos;
        }
        catch { }

        await CargarPostsLikesAsync();
    }

    // ── Carga y filtra los posts con meGusta == true ──────────────────────────

    private async Task CargarPostsLikesAsync()
    {
        Cargando.IsVisible = true;
        Cargando.IsRunning = true;
        ListaPosts.IsVisible = false;

        _posts.Clear();

        try
        {
            var items = await _api.GetPublicacionesAsync(pagina: 1, por: 100);

            foreach (var p in items)
            {
                try
                {
                    bool meGusta = p.TryGetProperty("meGusta", out var mg) && mg.GetBoolean();
                    if (!meGusta) continue;

                    int id = p.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;

                    string nombre = p.TryGetProperty("usuarioNombre", out var n)
                        ? n.GetString() ?? "Usuario" : "Usuario";
                    string inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "U";

                    string imagenUrl = string.Empty;
                    string? imgStr = null;
                    if (p.TryGetProperty("imagen", out var img) && img.ValueKind == System.Text.Json.JsonValueKind.String)
                        imgStr = img.GetString();
                    else if (p.TryGetProperty("imagenUrl", out var imgUrl) && imgUrl.ValueKind == System.Text.Json.JsonValueKind.String)
                        imgStr = imgUrl.GetString();
                    else if (p.TryGetProperty("imagenBase64", out var b64) && b64.ValueKind == System.Text.Json.JsonValueKind.String)
                        imgStr = b64.GetString();

                    if (!string.IsNullOrWhiteSpace(imgStr))
                    {
                        imagenUrl = (imgStr.StartsWith("http://") || imgStr.StartsWith("https://"))
    ? imgStr
    : imgStr.StartsWith("data:")
        ? imgStr
        : $"data:image/jpeg;base64,{imgStr}";
                    }
                    else if (p.TryGetProperty("imagenBase64", out var b64)
                             && b64.GetString() is string b64str && !string.IsNullOrEmpty(b64str))
                    {
                        imagenUrl = b64str.StartsWith("data:") ? b64str : $"data:image/jpeg;base64,{b64str}";
                    }

                    string descripcion = p.TryGetProperty("descripcion", out var desc)
                        ? desc.GetString() ?? "" : "";
                    int numLikes = p.TryGetProperty("numLikes", out var nl2) ? nl2.GetInt32() : 0;
                    int numCom   = p.TryGetProperty("numComentarios", out var nc) ? nc.GetInt32() : 0;

                    string usuarioId = p.TryGetProperty("usuarioId", out var uid)
                        ? (uid.ValueKind == System.Text.Json.JsonValueKind.Number
                            ? uid.GetInt32().ToString()
                            : uid.GetString() ?? "") : "";

                    string tiempo = "Hace un momento";
                    if (p.TryGetProperty("fechaCreacion", out var fecha)
                        && DateTime.TryParse(fecha.GetString(), out var dt))
                    {
                        var diff = DateTime.Now - dt.ToLocalTime();
                        tiempo = diff.TotalMinutes < 2  ? "Hace unos segundos"
                               : diff.TotalMinutes < 60 ? $"Hace {(int)diff.TotalMinutes} min"
                               : diff.TotalHours < 24   ? $"Hace {(int)diff.TotalHours} h"
                               : diff.TotalDays  < 2    ? "Ayer"
                               : $"Hace {(int)diff.TotalDays} días";
                    }

                    _posts.Add(new MomentoItem
                    {
                        Id             = id,
                        UsuarioNombre  = nombre,
                        UsuarioInicial = inicial,
                        TiempoPublicado = tiempo,
                        Descripcion    = descripcion,
                        ImagenUrl      = imagenUrl,
                        NumLikes       = numLikes,
                        NumComentarios = numCom,
                        MeGusta        = true,
                        EsMio          = !string.IsNullOrEmpty(_miId) && usuarioId == _miId,
                        UsuarioId      = usuarioId
                    });
                }
                catch { }
            }
        }
        catch { }

        Cargando.IsVisible = false;
        Cargando.IsRunning = false;
        ListaPosts.IsVisible = true;

        LabelConteo.Text = _posts.Count > 0
            ? $"{_posts.Count} {(_posts.Count == 1 ? "post" : "posts")}"
            : "";

        ListaPosts.ItemsSource = null;
        ListaPosts.ItemsSource = _posts;
    }

    // ── Tap en tarjeta → confirmar quitar like ───────────────────────────────

    private async void OnPostTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem post) return;

        bool confirmar = await DisplayAlert(
            "Quitar like",
            "¿Quieres quitar el ❤️ de este post?",
            "Sí, quitar", "Cancelar");

        if (!confirmar) return;

        var (nuevoMeGusta, _) = await _api.ToggleLikePublicacionAsync(post.Id);

        if (!nuevoMeGusta)
        {
            _posts.Remove(post);
            LabelConteo.Text = _posts.Count > 0
                ? $"{_posts.Count} {(_posts.Count == 1 ? "post" : "posts")}"
                : "";
            ListaPosts.ItemsSource = null;
            ListaPosts.ItemsSource = _posts;
        }
    }

    // ── Bottom nav ───────────────────────────────────────────────────────────

    private async void OnBuscarTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private void OnFavoritosTapped(object sender, EventArgs e) { }

    private async void OnReservasTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}
