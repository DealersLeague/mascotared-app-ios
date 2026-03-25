using Mascotared.Services;

namespace Mascotared;

public partial class Cuidadores : ContentPage
{
    private List<CuidadorItem> _todos = new();
    private decimal _miMedia = 0m;
    private int _miTotal = 0;

    public Cuidadores()
    {
        InitializeComponent();
        _ = CargarDatosAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            var resultado = await new ApiService().GetMisReviewsAsync();
            if (resultado is not null && resultado.Total > 0)
            {
                _miMedia = (decimal)resultado.Media;
                _miTotal = resultado.Total;
            }
        }
        catch { }

        var tasks = await TasksRepository.Instance.GetAllAsync();

        _todos = tasks
            .Where(t => t.caretaker
                && !t.IsFinished
                && (string.IsNullOrEmpty(t.AceptadoPorId) || t.EsMia || t.EsAceptado))
            .GroupBy(t => string.IsNullOrEmpty(t.UsuarioIdPublico) ? t.name : t.UsuarioIdPublico)
            .Select(g => MapearCuidador(g.OrderByDescending(x => x.ValoracionAutor).First()))
            .ToList();

        ActualizarLista(_todos);
    }

    private CuidadorItem MapearCuidador(TaskItem t)
    {
        var u = UsuarioService.Instancia.Usuario;
        bool esMio = !string.IsNullOrEmpty(t.name) && t.name == u.Nombre;

        return new CuidadorItem
        {
            Id = t.offerId,
            UsuarioId = t.UsuarioIdPublico ?? string.Empty,   // UserId real (GUID) para el chat
            Nombre = string.IsNullOrEmpty(t.name) ? "Usuario" : t.name,
            Edad = t.age,
            Localizacion = t.userLocation,
            Verificado = esMio && u.Verificado,
            Online = false,
            Tag = "Cuidador/a",
            UltimaActividad = "Recientemente",
            DescripcionPersonal = t.description,
            TarifaPorHora = t.perHour,
            DiasDisponibles = t.weekDays.Count > 0 ? string.Join(",", t.weekDays) : string.Empty,
            FranjasHorarias = t.timeOfDay.Count > 0 ? string.Join(",", t.timeOfDay) : string.Empty,
            TieneMascotas = t.hadPets,
            NumeroMascotasACuidar = t.maxPets,
            TiposAnimalACuidar = t.canLookafter.ToList(),
            HaCuidadoNecesidadesEspeciales = t.specialNeeds,
            PuedeCuidarNecesidadesEspeciales = t.specialNeeds,
            Valoracion = esMio ? _miMedia : (decimal)t.ValoracionAutor,
            NumeroResenas = esMio ? _miTotal : (t.ValoracionAutor > 0 ? 1 : 0),
            NumeroReservas = t.completedTasks,
            FotoPerfil = string.IsNullOrEmpty(t.photoPath) ? null : t.photoPath,
        };
    }

    private void ActualizarLista(List<CuidadorItem> lista)
    {
        ListaCuidadores.ItemsSource = lista;
        LblResultados.Text = $"Ver +{lista.Count} resultados";
    }

    private void OnBusquedaChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.ToLower() ?? string.Empty;
        var filtrada = string.IsNullOrWhiteSpace(texto)
            ? _todos
            : _todos.Where(c =>
                c.Nombre.ToLower().Contains(texto) ||
                (c.Localizacion?.ToLower().Contains(texto) ?? false))
              .ToList();
        ActualizarLista(filtrada);
    }

    private async void OnTarjetaTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CuidadorItem cuidador)
            await Navigation.PushModalAsync(new CuidadoresPopup(cuidador), animated: true);
    }

    private async void OnNuevoCuidadorTapped(object? sender, TappedEventArgs e)
        => await DisplayAlertAsync("Próximamente", "Formulario de nuevo cuidador.", "OK");

    private async void OnBuscarTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}

// ── Modelo ────────────────────────────────────────────────────────────────────

public class CuidadorItem
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;   // UserId real (GUID) para el chat
    public string Nombre { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string? Localizacion { get; set; }
    public bool Verificado { get; set; }
    public bool Online { get; set; }
    public string UltimaActividad { get; set; } = string.Empty;
    public string? DescripcionPersonal { get; set; }
    public decimal TarifaPorHora { get; set; }
    public string Tag { get; set; } = "Cuidador";
    public string? DiasDisponibles { get; set; }
    public string? FranjasHorarias { get; set; }
    public bool TieneMascotas { get; set; }
    public int NumeroMascotasACuidar { get; set; }
    public List<string> TiposAnimalACuidar { get; set; } = new();
    public bool HaCuidadoNecesidadesEspeciales { get; set; }
    public bool PuedeCuidarNecesidadesEspeciales { get; set; }
    public decimal Valoracion { get; set; }
    public int NumeroResenas { get; set; }
    public int NumeroReservas { get; set; }
    public int NumeroLikes { get; set; }
    public double DistanciaKm { get; set; }
    public string? FotoPerfil { get; set; }

    public string Inicial => string.IsNullOrEmpty(Nombre) ? "?" : Nombre[0].ToString().ToUpper();
    public string TarifaTxt => $"{TarifaPorHora:F2} €/hora";
    public string ResenasTxt => NumeroResenas == 0 ? "0 reseñas" : $"({NumeroResenas})";
    public string DistanciaTexto => DistanciaKm > 0 ? $"{DistanciaKm:F1} km" : "";
    public string TieneMascotasTxt => TieneMascotas ? "Sí" : "No";
    public string AnimalesTxt => TiposAnimalACuidar.Count > 0
        ? string.Join(", ", TiposAnimalACuidar) : "Cualquiera";
    public string NecesidadesEspecialesTxt => HaCuidadoNecesidadesEspeciales
        ? "Con experiencia en necesidades especiales" : "Sin experiencia previa";
    public string PuedeNecesidadesTxt =>
        PuedeCuidarNecesidadesEspeciales ? "Sí puede" : "No puede";

    public List<string> ListaDias =>
        DiasDisponibles?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
    public List<string> ListaFranjas =>
        FranjasHorarias?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
}