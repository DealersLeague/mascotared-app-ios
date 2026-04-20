using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Mascotared.Services;

namespace Mascotared;

public partial class MainPage : ContentPage
{
    private List<CuidadorItem> _cuidadores = new();
    private List<MascotaDestacada> _mascotas = new();
    private List<MomentoItem> _feed = new();
    private Location? _ubicacionActual;
	private bool _cargado = false;

    private readonly Mascotared.Services.ApiService _api = new();

    public MainPage()
    {
        InitializeComponent();
    }

protected override async void OnAppearing()
{
    base.OnAppearing();

    if (!_cargado)
    {
        _cargado = true;
        try
        {
            await CargarTodoAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error carga inicial: {ex.Message}");
        }
    }

    try
    {
        var convs = await _api.GetConversacionesAsync();
        BadgeNoLeidos.IsVisible = convs.Any(j =>
            j.TryGetProperty("noLeidos", out var nl) && nl.GetInt32() > 0);
    }
    catch { }
}

    // ── Carga inicial ─────────────────────────────────────────────────────────

    private async Task CargarTodoAsync()
    {
        // Cuidadores primero: CargarMascotasPublicasAsync necesita _cuidadores ya cargado
        await CargarCuidadoresYUbicacion();
        await Task.WhenAll(
            CargarFeedAsync(),
            CargarMascotasPublicasAsync()
        );
    }

    // ── Ubicación + Cuidadores ────────────────────────────────────────────────

private async Task CargarCuidadoresYUbicacion()
{
    try
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        // NO pedir permiso automáticamente al abrir Home
        if (status == PermissionStatus.Granted)
        {
            _ubicacionActual = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });
        }
        else
        {
            _ubicacionActual = null;
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MainPage] Ubicación: {ex.Message}");
        _ubicacionActual = null;
    }

    await CargarCuidadoresCercanosAsync();
}

    private async Task CargarCuidadoresCercanosAsync()
    {
        try
        {
            var tasks = await TasksRepository.Instance.GetAllAsync(esCuidador: true);

            var todos = tasks
            .GroupBy(t => string.IsNullOrEmpty(t.UsuarioIdPublico) ? t.name : t.UsuarioIdPublico)
            .Select(g => g.OrderByDescending(x => x.ValoracionAutor).First())
            .Select(t =>
            {
                double distancia = 0;
                if (_ubicacionActual != null && t.latitud.HasValue && t.longitud.HasValue)
                {
                    var ubicOferta = new Location(t.latitud.Value, t.longitud.Value);
                    distancia = Location.CalculateDistance(
                        _ubicacionActual, ubicOferta, DistanceUnits.Kilometers);
                }

                return new CuidadorItem
                {
                    Id = t.offerId,
                    UsuarioId = t.UsuarioIdPublico ?? string.Empty,
                    Nombre = t.name,
                    Localizacion = !string.IsNullOrEmpty(t.userLocation) ? t.userLocation : t.LocAutor,
                    FotoPerfil = t.FotoAutor,
                    Edad = t.age,
                    Tag = "Cuidador/a",
                    TarifaPorHora = t.perHour,
                    DescripcionPersonal = t.description,
                    DiasDisponibles = t.weekDays.Count > 0 ? string.Join(",", t.weekDays) : null,
                    FranjasHorarias = t.timeOfDay.Count > 0 ? string.Join(",", t.timeOfDay) : null,
                    TieneMascotas = t.hadPets,
                    NumeroMascotasACuidar = t.maxPets,
                    TiposAnimalACuidar = t.canLookafter.ToList(),
                    HaCuidadoNecesidadesEspeciales = t.specialNeeds,
                    PuedeCuidarNecesidadesEspeciales = t.specialNeeds,
                    NumeroReservas = t.completedTasks,
                    NumeroLikes = t.favorites,
                    DistanciaKm = distancia,
                    Valoracion = t.ValoracionAutor,
                    NumeroResenas = t.ValoracionAutor > 0 ? 1 : 0,
                };
            }).ToList();

            _cuidadores = _ubicacionActual != null
                ? todos.Where(c => c.DistanciaKm <= 5.0).OrderBy(c => c.DistanciaKm).ToList()
                : todos;

            ListaCuidadores.ItemsSource = _cuidadores;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error cargando cuidadores: {ex.Message}");
            ListaCuidadores.ItemsSource = new List<CuidadorItem>();
        }
    }

    // ── Feed desde API ────────────────────────────────────────────────────────

    private async Task CargarFeedAsync()
    {
        try
        {
            var items = await _api.GetPublicacionesAsync(pagina: 1, por: 20);

            _feed = items.Select((p, idx) =>
            {
                string nombre = p.TryGetProperty("usuarioNombre", out var n)
                    ? n.GetString() ?? "Usuario" : "Usuario";
                string inicial = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "U";

                string imagenUrl = string.Empty;
                string? imgStr = null;
                if (p.TryGetProperty("imagen", out var imgProp) && imgProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    imgStr = imgProp.GetString();
                else if (p.TryGetProperty("imagenUrl", out var imgUrlProp) && imgUrlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    imgStr = imgUrlProp.GetString();
                else if (p.TryGetProperty("imagenBase64", out var imgB64Prop) && imgB64Prop.ValueKind == System.Text.Json.JsonValueKind.String)
                    imgStr = imgB64Prop.GetString();

                if (!string.IsNullOrWhiteSpace(imgStr))
                {
                    if (imgStr.StartsWith("http://") || imgStr.StartsWith("https://"))
                        imagenUrl = imgStr;
                    else
                        imagenUrl = imgStr.StartsWith("data:") ? imgStr : $"data:image/jpeg;base64,{imgStr}";

                    System.Diagnostics.Debug.WriteLine($"[FEED] OK len={imagenUrl.Length} start={imagenUrl.Substring(0, Math.Min(60, imagenUrl.Length))}");
                }
                else
                {
                    var props = string.Join(", ", p.EnumerateObject().Select(x => x.Name));
                    System.Diagnostics.Debug.WriteLine($"[FEED] SIN IMAGEN props={props}");
                }

                string descripcion = p.TryGetProperty("descripcion", out var desc)
                    ? desc.GetString() ?? "" : "";
                int numLikes = p.TryGetProperty("numLikes", out var nl) ? nl.GetInt32() : 0;
                int numCom = p.TryGetProperty("numComentarios", out var nc) ? nc.GetInt32() : 0;
                bool meGusta = p.TryGetProperty("meGusta", out var mg) && mg.GetBoolean();
                int id = p.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : idx + 1;

                string tiempo = "Hace un momento";
                if (p.TryGetProperty("fechaCreacion", out var fecha)
                    && DateTime.TryParse(fecha.GetString(), out var dt))
                {
                    var diff = DateTime.UtcNow - dt.ToLocalTime();
                    tiempo = diff.TotalMinutes < 2 ? "Hace unos segundos"
                           : diff.TotalMinutes < 60 ? $"Hace {(int)diff.TotalMinutes} min"
                           : diff.TotalHours < 24 ? $"Hace {(int)diff.TotalHours} h"
                           : diff.TotalDays < 2 ? "Ayer"
                           : $"Hace {(int)diff.TotalDays} días";
                }

                string usuarioId = p.TryGetProperty("usuarioId", out var uid)
                    ? (uid.ValueKind == System.Text.Json.JsonValueKind.Number
                        ? uid.GetInt32().ToString()
                        : uid.GetString() ?? "") : "";
                string usuarioFoto = p.TryGetProperty("usuarioFoto", out var uf)
                    ? uf.GetString() ?? "" : "";

                string miId = Preferences.Get("user_id", "");

                return new MomentoItem
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
                    EsFavorito = false,
                    EsMio = !string.IsNullOrEmpty(miId) && usuarioId == miId,
                    UsuarioFoto = usuarioFoto,
                    UsuarioId = usuarioId,
                };
            }).ToList();

            ListaFeed.ItemsSource = _feed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error cargando feed: {ex.Message}");
            ListaFeed.ItemsSource = new List<MomentoItem>();
        }
    }

    // ── Mascotas públicas desde API ───────────────────────────────────────────

    private async Task CargarMascotasPublicasAsync()
    {
        try
        {
            // Carga TODAS las mascotas públicas del endpoint /Mascota/publicas
            var todasMascotas = await _api.GetMascotasPublicasAsync();

            var todasMascotasLista = new List<MascotaDestacada>();
            foreach (var m in todasMascotas)
{
    string nombre  = m.TryGetProperty("nombre", out var n) ? n.GetString() ?? "Mascota" : "Mascota";
    string especie = m.TryGetProperty("especie", out var esp) ? esp.GetString() ?? "" : "";

    string dueno = "";
    if (m.TryGetProperty("duenoNombre", out var dn) && dn.ValueKind == System.Text.Json.JsonValueKind.String)
        dueno = dn.GetString() ?? "";
    if (string.IsNullOrWhiteSpace(dueno)) dueno = "Propietario/a";

    string duenoId = "";
    if (m.TryGetProperty("duenoId", out var did))
        duenoId = did.ValueKind == System.Text.Json.JsonValueKind.String
            ? did.GetString() ?? "" : "";

    string imagenUrl = "";
    string? fotoStr = null;
    if (m.TryGetProperty("foto", out var foto) && foto.ValueKind == System.Text.Json.JsonValueKind.String)
        fotoStr = foto.GetString();
    else if (m.TryGetProperty("fotoUrl", out var fotoUrlProp) && fotoUrlProp.ValueKind == System.Text.Json.JsonValueKind.String)
        fotoStr = fotoUrlProp.GetString();
    else if (m.TryGetProperty("imagen", out var imagenProp) && imagenProp.ValueKind == System.Text.Json.JsonValueKind.String)
        fotoStr = imagenProp.GetString();

    if (!string.IsNullOrWhiteSpace(fotoStr))
    {
        if (fotoStr.StartsWith("http://") || fotoStr.StartsWith("https://"))
            imagenUrl = fotoStr;
        else if (fotoStr.StartsWith("data:"))
            imagenUrl = fotoStr;
        else
            imagenUrl = $"data:image/jpeg;base64,{fotoStr}";
    }

    todasMascotasLista.Add(new MascotaDestacada
    {
        Nombre     = nombre,
        Dueno      = dueno,
        DuenoId    = duenoId,
        TipoAnimal = especie,
        ImagenUrl  = imagenUrl,
    });
}

            // Deduplicar: si el backend tiene entradas repetidas del mismo dueño+mascota,
            // quedarse solo con la primera aparición (el backend ya ordena por FechaCreacion desc)
            var vistos = new HashSet<string>();
            _mascotas = new List<MascotaDestacada>();
            foreach (var mascota in todasMascotasLista)
            {
                string clave = $"{mascota.DuenoId}|{mascota.Nombre}";
                if (vistos.Add(clave))
                    _mascotas.Add(mascota);
            }

            ListaMascotas.ItemsSource = _mascotas;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error cargando mascotas: {ex.Message}");
            _mascotas = new List<MascotaDestacada>();
            ListaMascotas.ItemsSource = _mascotas;
        }
    }

    // ── FEED — acciones ───────────────────────────────────────────────────────

    private async void OnAutorTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem m) return;
        await Navigation.PushAsync(new Mascotared.Perfil.PerfilPublico(m.UsuarioId, m.UsuarioFoto, m.UsuarioNombre));
    }

    private async void OnLikeFeedTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem m) return;

        try
        {
            var (meGusta, numLikes) = await _api.ToggleLikePublicacionAsync(m.Id);
            m.MeGusta = meGusta;
            m.NumLikes = numLikes;
        }
        catch
        {
            m.MeGusta = !m.MeGusta;
            m.NumLikes += m.MeGusta ? 1 : -1;
        }

        var lista = ListaFeed.ItemsSource as List<MomentoItem>;
        ListaFeed.ItemsSource = null;
        ListaFeed.ItemsSource = lista;
    }

    private async void OnComentarTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is MomentoItem momento)
            await Navigation.PushAsync(new ComentariosPage(momento), animated: true);
    }

    private async void OnGuardarFotoTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem momento) return;
        try
        {
            byte[] imageBytes;

            if (momento.ImagenUrl.StartsWith("data:image"))
            {
                var base64 = momento.ImagenUrl[(momento.ImagenUrl.IndexOf(',') + 1)..];
                base64 = base64.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
                imageBytes = Convert.FromBase64String(base64);
            }
            else if (momento.ImagenUrl.StartsWith("file://") || momento.ImagenUrl.StartsWith("/"))
            {
                imageBytes = await File.ReadAllBytesAsync(momento.ImagenUrl.Replace("file://", ""));
            }
            else
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
                };
                using var http = new HttpClient(handler);
                imageBytes = await http.GetByteArrayAsync(momento.ImagenUrl);
            }

            var fileName = $"mascotared_{momento.Id}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);

#if ANDROID
            var mediaStore = new AndroidMediaStoreHelper();
            var savedPath = await mediaStore.SaveImageToGallery(filePath, fileName);
            if (savedPath != null)
                await DisplayAlertAsync("✅ Guardado", "Foto guardada en la galería", "OK");
            else
                await DisplayAlertAsync("Error", "No se pudo guardar la foto", "OK");
#elif WINDOWS || IOS || MACCATALYST
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var finalPath = Path.Combine(documentsPath, fileName);
            await File.WriteAllBytesAsync(finalPath, imageBytes);
            await DisplayAlertAsync("✅ Guardado", $"Foto guardada en: {finalPath}", "OK");
#else
            await DisplayAlertAsync("✅ Guardado", "Foto descargada", "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo guardar la foto: {ex.Message}", "OK");
        }
    }

    private async void OnOpcionesPostTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not MomentoItem m) return;

        if (m.EsMio)
        {
            string accion = await DisplayActionSheetAsync(
                "Opciones", "Cancelar", "🗑️ Eliminar publicación",
                "✏️ Editar descripción");

            if (accion == "✏️ Editar descripción")
            {
                string? nuevaDesc = await DisplayPromptAsync(
                    "Editar descripción",
                    "Escribe la nueva descripción:",
                    initialValue: m.Descripcion,
                    maxLength: 300,
                    keyboard: Keyboard.Text);

                if (nuevaDesc == null) return;

                bool ok = await _api.ActualizarDescripcionPublicacionAsync(m.Id, nuevaDesc.Trim());
                if (ok)
                {
                    m.Descripcion = nuevaDesc.Trim();
                    var lista = ListaFeed.ItemsSource as List<MomentoItem>;
                    ListaFeed.ItemsSource = null;
                    ListaFeed.ItemsSource = lista;
                }
                else
                    await DisplayAlertAsync("Error", "No se pudo guardar la descripción.", "OK");
            }
            else if (accion == "🗑️ Eliminar publicación")
            {
                bool confirmar = await DisplayAlert(
                    "Eliminar", "¿Seguro que quieres eliminar esta publicación?",
                    "Eliminar", "Cancelar");
                if (!confirmar) return;

                bool ok = await _api.EliminarPublicacionAsync(m.Id);
                if (ok)
                {
                    _feed.Remove(m);
                    ListaFeed.ItemsSource = null;
                    ListaFeed.ItemsSource = _feed;
                }
                else
                    await DisplayAlertAsync("Error", "No se pudo eliminar la publicación.", "OK");
            }
        }
        else
        {
            string accion = await DisplayActionSheetAsync(
                "Opciones", "Cancelar", null,
                "🚩 Reportar publicación");

            if (accion == "🚩 Reportar publicación")
                await DisplayAlertAsync("Reportado", "Gracias por tu reporte. Lo revisaremos.", "OK");
        }
    }

    private async void OnAnadirFotoTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new NuevoMomento(async momento =>
        {
            string imagenParaApi = momento.ImagenUrl ?? string.Empty;
            if (!string.IsNullOrEmpty(imagenParaApi))
            {
                if (imagenParaApi.StartsWith("data:"))
                {
                    var b64 = imagenParaApi[(imagenParaApi.IndexOf(',') + 1)..];
                    var bytes = Convert.FromBase64String(b64);
                    imagenParaApi = await ComprimirImagenBase64Async(bytes);
                }
                else if (imagenParaApi.StartsWith("/") || imagenParaApi.StartsWith("file://"))
                {
                    var path = imagenParaApi.Replace("file://", "");
                    var bytes = await File.ReadAllBytesAsync(path);
                    imagenParaApi = await ComprimirImagenBase64Async(bytes);
                }
            }
            var idReal = await _api.CrearPublicacionAsync(imagenParaApi, momento.Descripcion);
            if (idReal > 0) momento.Id = idReal;

            // Recargar feed completo para mostrar la publicación real de BD
            await CargarFeedAsync();

        }), animated: true);
    }

    // ── CUIDADORES ────────────────────────────────────────────────────────────

    private async void OnCuidadorTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is CuidadorItem c)
            await Navigation.PushModalAsync(new CuidadoresPopup(c), animated: true);
    }

    private async void OnVerTodosCuidadoresTapped(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new CuidadoresNear(), animated: true);

    // ── MASCOTAS ─────────────────────────────────────────────────────────────

    private async void OnVerTodasMascotasTapped(object? sender, TappedEventArgs e)
    {
        // Si los datos aún no han cargado (fire-and-forget inicial), recargar antes de navegar
        if (_mascotas.Count == 0)
        {
            // Asegurar que _cuidadores esté cargado antes de pedir sus mascotas
            if (_cuidadores.Count == 0)
                await CargarCuidadoresYUbicacion();
            await CargarMascotasPublicasAsync();
        }
        await Navigation.PushAsync(new MascotasPorDuenoPage(_mascotas), animated: true);
    }

    private async void OnMomentosTapped(object? sender, TappedEventArgs e)
        => await Navigation.PushAsync(new Momentos(), animated: true);

    // ── BOTTOM NAV ────────────────────────────────────────────────────────────

    private void OnBuscarTapped(object? sender, EventArgs e) { }

    // ── Comprimir imagen antes de subir ──────────────────────────────────────

    private static async Task<string> ComprimirImagenBase64Async(byte[] originalBytes, int calidadJpeg = 60, int maxDimension = 1080)
    {
        try
        {
            using var inputStream = new MemoryStream(originalBytes);
            var image = PlatformImage.FromStream(inputStream);
            if (image == null) return Convert.ToBase64String(originalBytes);

            float w = image.Width, h = image.Height;
            Microsoft.Maui.Graphics.IImage resized;
            if (w > maxDimension || h > maxDimension)
            {
                float scale = Math.Min(maxDimension / w, maxDimension / h);
                resized = image.Resize(w * scale, h * scale, ResizeMode.Bleed);
            }
            else
            {
                resized = image;
            }

            using var outputStream = new MemoryStream();
            await resized.SaveAsync(outputStream, ImageFormat.Jpeg, (float)calidadJpeg / 100f);
            return Convert.ToBase64String(outputStream.ToArray());
        }
        catch
        {
            return Convert.ToBase64String(originalBytes);
        }
    }

    private async void OnFavoritosTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}