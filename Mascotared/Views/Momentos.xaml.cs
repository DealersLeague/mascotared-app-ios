using Mascotared.Services;

namespace Mascotared;

public partial class Momentos : ContentPage
{
    private List<MomentoItem> _publicados = new();
    private List<MomentoItem> _relacionados = new();
    private bool _mostrandoPublicados = true;
    private bool _recienPublicado = false;
    private readonly ApiService _api = new();
    private readonly string _miId = Preferences.Get("user_id", "");

    public Momentos()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_recienPublicado)
        {
            _recienPublicado = false;
            return;
        }
        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            var items = await _api.GetPublicacionesAsync(pagina: 1, por: 50);

            _publicados.Clear();
            _relacionados.Clear();

            foreach (var p in items)
            {
                try
                {
                    string nombre = p.TryGetProperty("usuarioNombre", out var n)
                        ? n.GetString() ?? "Usuario" : "Usuario";
                    string inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "U";

                    string imagenUrl = string.Empty;
                    if (p.TryGetProperty("imagen", out var img) && img.GetString() is string imgStr && !string.IsNullOrEmpty(imgStr))
                        imagenUrl = imgStr;
                    else if (p.TryGetProperty("imagenBase64", out var b64) && b64.GetString() is string b64str && !string.IsNullOrEmpty(b64str))
                        imagenUrl = b64str.StartsWith("data:") ? b64str : $"data:image/jpeg;base64,{b64str}";

                    string descripcion = p.TryGetProperty("descripcion", out var desc)
                        ? desc.GetString() ?? "" : "";
                    int numLikes = p.TryGetProperty("numLikes", out var nl) ? nl.GetInt32() : 0;
                    int numCom = p.TryGetProperty("numComentarios", out var nc) ? nc.GetInt32() : 0;
                    bool meGusta = p.TryGetProperty("meGusta", out var mg) && mg.GetBoolean();
                    int id = p.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;

                    string usuarioId = p.TryGetProperty("usuarioId", out var uid)
                        ? (uid.ValueKind == System.Text.Json.JsonValueKind.Number
                            ? uid.GetInt32().ToString()
                            : uid.GetString() ?? "") : "";

                    string usuarioFoto = p.TryGetProperty("usuarioFoto", out var uf)
                        ? uf.GetString() ?? "" : "";

                    string tiempo = "Hace un momento";
                    if (p.TryGetProperty("fechaCreacion", out var fecha)
                        && DateTime.TryParse(fecha.GetString(), out var dt))
                    {
                        var diff = DateTime.Now - dt.ToLocalTime();
                        tiempo = diff.TotalMinutes < 2 ? "Hace unos segundos"
                               : diff.TotalMinutes < 60 ? $"Hace {(int)diff.TotalMinutes} min"
                               : diff.TotalHours < 24 ? $"Hace {(int)diff.TotalHours} h"
                               : diff.TotalDays < 2 ? "Ayer"
                               : $"Hace {(int)diff.TotalDays} días";
                    }

                    var item = new MomentoItem
                    {
                        Id = id,
                        UsuarioNombre = nombre,
                        UsuarioInicial = inicial,
                        TiempoPublicado = tiempo,
                        Descripcion = descripcion,
                        ImagenUrl = imagenUrl,
                        NumLikes = numLikes,
                        NumComentarios = numCom,
                        MeGusta = meGusta,
                        EsMio = !string.IsNullOrEmpty(_miId) && usuarioId == _miId,
                        UsuarioFoto = usuarioFoto
                    };

                    if (item.EsMio) _publicados.Add(item);
                    else _relacionados.Add(item);
                }
                catch { }
            }
        }
        catch { }

        MostrarTab(_mostrandoPublicados);
    }

    private void MostrarTab(bool publicados)
    {
        _mostrandoPublicados = publicados;
        ListaMomentos.ItemsSource = publicados ? _publicados : _relacionados;

        BtnPublicado.BackgroundColor = publicados ? Color.FromArgb("#455AEB") : Colors.Transparent;
        BtnRelacionada.BackgroundColor = publicados ? Colors.Transparent : Color.FromArgb("#455AEB");
        BtnPublicado.TextColor = publicados ? Colors.White : Color.FromArgb("#9AA0C4");
        BtnRelacionada.TextColor = publicados ? Color.FromArgb("#9AA0C4") : Colors.White;
    }

    private void OnPublicadoTapped(object sender, EventArgs e) => MostrarTab(true);
    private void OnRelacionadaTapped(object sender, EventArgs e) => MostrarTab(false);

    private async void OnNuevoMomentoTapped(object sender, TappedEventArgs e)
    {
        // ✅ _recienPublicado se marca dentro del callback, solo si realmente se publicó
        await Navigation.PushAsync(new NuevoMomento(item =>
        {
            _recienPublicado = true;
            _publicados.Insert(0, item);
            if (_mostrandoPublicados)
                ListaMomentos.ItemsSource = null;
            ListaMomentos.ItemsSource = _publicados;
        }));
    }

    private async void OnLikeTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem m) return;

        var (nuevoMeGusta, nuevoNumLikes) = await _api.ToggleLikePublicacionAsync(m.Id);
        m.MeGusta = nuevoMeGusta;
        m.NumLikes = nuevoNumLikes;

        var lista = _mostrandoPublicados ? _publicados : _relacionados;
        ListaMomentos.ItemsSource = null;
        ListaMomentos.ItemsSource = lista;
    }
}