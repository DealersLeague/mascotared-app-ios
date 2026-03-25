using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls;
using Mascotared.Services;

namespace Mascotared;

public partial class CuidadoresNear : ContentPage
{
    private List<CuidadorItem> _todos = new();
    private List<CuidadorItem> _filtrados = new();
    private Location? _ubicacionActual;
    private string _categoriaSeleccionada = "Todos";
    private bool _pageActive = true;

    public CuidadoresNear()
    {
        InitializeComponent();
        _ = CargarDatosAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _pageActive = true;
        // Si los datos ya cargaron, reaplicar filtros al volver de un popup/modal
        if (_todos.Count > 0)
            AplicarFiltros();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pageActive = false;
    }

    // ── Carga desde API ───────────────────────────────────────────────────────

    private async Task CargarDatosAsync()
    {
        // 1. Ubicación
        await ObtenerUbicacionActual();

        // 2. Ofertas de cuidador desde la API
        try
        {
            var tasks = await TasksRepository.Instance.GetAllAsync(esCuidador: true);
            string miId = Preferences.Get("user_id", string.Empty);

            _todos = tasks
            .Where(t => !string.IsNullOrEmpty(t.UsuarioIdPublico))
            .GroupBy(t => t.UsuarioIdPublico)
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
                    Edad = t.age,
                    // Mostrar localización del task; si está vacía, usar la del perfil del autor
                    Localizacion = !string.IsNullOrEmpty(t.userLocation) ? t.userLocation : t.LocAutor,
                    FotoPerfil = t.FotoAutor,
                    Verificado = false,
                    Online = false,
                    Tag = "Cuidador/a",
                    UltimaActividad = "Recientemente",
                    DescripcionPersonal = t.description,
                    TarifaPorHora = t.perHour,
                    DiasDisponibles = t.weekDays.Count > 0 ? string.Join(",", t.weekDays) : null,
                    FranjasHorarias = t.timeOfDay.Count > 0 ? string.Join(",", t.timeOfDay) : null,
                    TieneMascotas = t.hadPets,
                    NumeroMascotasACuidar = t.maxPets,
                    TiposAnimalACuidar = t.canLookafter.ToList(),
                    HaCuidadoNecesidadesEspeciales = t.specialNeeds,
                    PuedeCuidarNecesidadesEspeciales = t.specialNeeds,
                    NumeroReservas = t.completedTasks,
                    NumeroLikes = t.favorites,
                    Valoracion = t.ValoracionAutor,
                    NumeroResenas = t.ValoracionAutor > 0 ? 1 : 0,
                    DistanciaKm = distancia,
                };
            })
            .OrderBy(c => c.DistanciaKm)
            .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CuidadoresNear] Error cargando: {ex.Message}");
            _todos = new();
        }

        AplicarFiltros();
    }

    // ── Ubicación ─────────────────────────────────────────────────────────────

    private async Task ObtenerUbicacionActual()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                _ubicacionActual = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CuidadoresNear] Ubicación: {ex.Message}");
        }
    }

    // ── Filtros ───────────────────────────────────────────────────────────────

    private void AplicarFiltros()
    {
        if (!_pageActive) return;
        _filtrados = _todos.ToList();

        switch (_categoriaSeleccionada)
        {
            case "Cercanos":
                // Muestra solo los que están a ≤ 5 km
                // Si no hay ubicación muestra todos
                if (_ubicacionActual != null)
                    _filtrados = _filtrados.Where(c => c.DistanciaKm <= 5).ToList();
                break;
            case "Verificados":
                _filtrados = _filtrados.Where(c => c.Verificado).ToList();
                break;
            case "Online":
                _filtrados = _filtrados.Where(c => c.Online).ToList();
                break;
        }

        var textoBusqueda = EntryBusqueda.Text?.ToLower() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(textoBusqueda))
        {
            _filtrados = _filtrados.Where(c =>
                c.Nombre.ToLower().Contains(textoBusqueda) ||
                (c.Localizacion?.ToLower().Contains(textoBusqueda) ?? false) ||
                c.Tag.ToLower().Contains(textoBusqueda))
              .ToList();
        }

        ListaCuidadores.ItemsSource = _filtrados;
    }

    private void OnBusquedaChanged(object? sender, TextChangedEventArgs e)
        => AplicarFiltros();

    private void OnCategoriaTodosTapped(object? sender, EventArgs e)
    {
        _categoriaSeleccionada = "Todos";
        ActualizarCategoriasUI();
        AplicarFiltros();
    }

    private void OnCategoriaCercanosTapped(object? sender, EventArgs e)
    {
        _categoriaSeleccionada = "Cercanos";
        ActualizarCategoriasUI();
        AplicarFiltros();
    }

    private void OnCategoriaVerificadosTapped(object? sender, EventArgs e)
    {
        _categoriaSeleccionada = "Verificados";
        ActualizarCategoriasUI();
        AplicarFiltros();
    }

    private void OnCategoriaOnlineTapped(object? sender, EventArgs e)
    {
        _categoriaSeleccionada = "Online";
        ActualizarCategoriasUI();
        AplicarFiltros();
    }

    private void ActualizarCategoriasUI()
    {
        FrameTodos.BackgroundColor = _categoriaSeleccionada == "Todos"
            ? Color.FromArgb("#455AEB") : Color.FromArgb("#F4F6FA");
        ((Label)FrameTodos.Content!).TextColor = _categoriaSeleccionada == "Todos"
            ? Colors.White : Color.FromArgb("#2C2C2C");

        FrameCercanos.BackgroundColor = _categoriaSeleccionada == "Cercanos"
            ? Color.FromArgb("#455AEB") : Color.FromArgb("#F4F6FA");
        ((Label)FrameCercanos.Content!).TextColor = _categoriaSeleccionada == "Cercanos"
            ? Colors.White : Color.FromArgb("#2C2C2C");

        FrameVerificados.BackgroundColor = _categoriaSeleccionada == "Verificados"
            ? Color.FromArgb("#455AEB") : Color.FromArgb("#F4F6FA");
        ((Label)FrameVerificados.Content!).TextColor = _categoriaSeleccionada == "Verificados"
            ? Colors.White : Color.FromArgb("#2C2C2C");

        FrameOnline.BackgroundColor = _categoriaSeleccionada == "Online"
            ? Color.FromArgb("#455AEB") : Color.FromArgb("#F4F6FA");
        ((Label)FrameOnline.Content!).TextColor = _categoriaSeleccionada == "Online"
            ? Colors.White : Color.FromArgb("#2C2C2C");
    }

    private async void OnTarjetaTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is CuidadorItem cuidador)
            await Navigation.PushModalAsync(new CuidadoresPopup(cuidador), animated: true);
    }

    private async void OnBuscarTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}