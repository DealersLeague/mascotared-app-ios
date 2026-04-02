using System.Collections.ObjectModel;
using Mascotared.Services;
using System.Text.Json;

namespace Mascotared;

public partial class ComentariosPage : ContentPage
{
    private readonly MomentoItem _momento;
    private readonly ObservableCollection<ComentarioItem> _comentarios = new();
    private readonly ApiService _api = new();

    public ComentariosPage(MomentoItem momento)
    {
        InitializeComponent();
        _momento = momento;
        BindingContext = momento;
        ListaComentarios.ItemsSource = _comentarios;
        _ = CargarComentariosAsync();
    }

    // ── Carga desde API ───────────────────────────────────────────────────────

    private async Task CargarComentariosAsync()
    {
        try
        {
            var items = await _api.GetComentariosAsync(_momento.Id);

            _comentarios.Clear();
            foreach (var c in items)
            {
                string nombre = c.TryGetProperty("usuarioNombre", out var n)
                    ? n.GetString() ?? "Usuario" : "Usuario";
                string inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "U";
                string texto = c.TryGetProperty("contenido", out var ct)
                    ? ct.GetString() ?? "" : "";
                int id = c.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;

                string tiempo = "Hace un momento";
                if (c.TryGetProperty("fechaCreacion", out var fecha)
                    && DateTime.TryParse(fecha.GetString(), out var dt))
                {
                    var diff = DateTime.UtcNow - dt.ToUniversalTime();
                    tiempo = diff.TotalMinutes < 2 ? "Ahora mismo"
                           : diff.TotalMinutes < 60 ? $"Hace {(int)diff.TotalMinutes} min"
                           : diff.TotalHours < 24 ? $"Hace {(int)diff.TotalHours} h"
                           : diff.TotalDays < 2 ? "Ayer"
                           : $"Hace {(int)diff.TotalDays} días";
                }

                string? fotoUrl = c.TryGetProperty("usuarioFoto", out var fv)
                    && fv.ValueKind != JsonValueKind.Null ? fv.GetString() : null;

                _comentarios.Add(new ComentarioItem
                {
                    Id = id,
                    UsuarioNombre = nombre,
                    UsuarioInicial = inicial,
                    UsuarioFoto = fotoUrl ?? "",
                    Texto = texto,
                    TiempoPublicado = tiempo,
                    EsLikeado = false
                });
            }

            _momento.NumComentarios = _comentarios.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Comentarios] Error: {ex.Message}");
        }
    }

    // ── Enviar comentario ─────────────────────────────────────────────────────

    private async void OnEnviarComentario(object sender, EventArgs e)
    {
        var texto = EntryComentario.Text?.Trim();
        if (string.IsNullOrWhiteSpace(texto)) return;

        EntryComentario.Text = string.Empty;

        try
        {
            var nuevo = await _api.ComentarPublicacionAsync(_momento.Id, texto);
            if (nuevo == null) return;

            // Desenvolver JsonElement? a JsonElement para poder usar TryGetProperty
            var nuevoEl = nuevo.Value;
            string nombre = nuevoEl.TryGetProperty("usuarioNombre", out var n)
                ? n.GetString() ?? UsuarioService.Instancia.Usuario.Nombre : UsuarioService.Instancia.Usuario.Nombre;
            string inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "U";
            int id = nuevoEl.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;

            string? miFoto = Preferences.Get("user_foto", string.Empty);
            if (string.IsNullOrEmpty(miFoto)) miFoto = null;

            _comentarios.Insert(0, new ComentarioItem
            {
                Id = id,
                UsuarioNombre = nombre,
                UsuarioInicial = inicial,
                UsuarioFoto = miFoto ?? "",
                Texto = texto,
                TiempoPublicado = "Ahora mismo",
                EsLikeado = false
            });

            _momento.NumComentarios = _comentarios.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Comentarios] Error enviando: {ex.Message}");
            await DisplayAlertAsync("Error", "No se pudo enviar el comentario.", "OK");
        }
    }

    // ── Like local (los likes de comentarios no están en la API) ─────────────

    private void OnLikeComentarioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ComentarioItem comentario) return;
        comentario.EsLikeado = !comentario.EsLikeado;
        ListaComentarios.ItemsSource = null;
        ListaComentarios.ItemsSource = _comentarios;
    }

    // ── Responder: pre-rellena el Entry con @nombre ────────────────────────────

    private void OnResponderComentarioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not ComentarioItem comentario) return;
        EntryComentario.Text = $"@{comentario.UsuarioNombre} ";
        EntryComentario.Focus();
        // Mover cursor al final
        EntryComentario.CursorPosition = EntryComentario.Text.Length;
    }
}

public class ComentarioItem
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioInicial { get; set; } = string.Empty;
    public string UsuarioFoto { get; set; } = string.Empty;
    public bool TieneFoto => !string.IsNullOrEmpty(UsuarioFoto) &&
                             (UsuarioFoto.StartsWith("http://") || UsuarioFoto.StartsWith("https://"));
    public bool NoTieneFoto => !TieneFoto;
    public string Texto { get; set; } = string.Empty;
    public string TiempoPublicado { get; set; } = string.Empty;
    public bool EsLikeado { get; set; }
}