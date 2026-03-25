using Mascotared.Services;

namespace Mascotared;

public partial class Propietario : ContentPage
{
    private List<PropietarioItem> _todos = new();

    private decimal _miMedia = 0m;
    private int _miTotal = 0;

    public Propietario()
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
            .Where(t => !t.caretaker
                && !t.IsFinished
                && (string.IsNullOrEmpty(t.AceptadoPorId) || t.EsMia || t.EsAceptado))
            .Select(MapearPropietario)
            .ToList();

        ActualizarLista(_todos);
    }

    private PropietarioItem MapearPropietario(TaskItem t)
    {
        var u = UsuarioService.Instancia.Usuario;
        bool esMio = !string.IsNullOrEmpty(t.name) && t.name == u.Nombre;

        return new PropietarioItem
        {
            Id = t.offerId,
            UsuarioId = t.UsuarioIdPublico ?? string.Empty,   // UserId real para el chat
            Nombre = string.IsNullOrEmpty(t.name) ? "Usuario" : t.name,
            Edad = t.age,
            Localizacion = t.userLocation,
            Verificado = esMio && u.Verificado,
            Online = false,
            UltimaActividad = "Recientemente",
            DescripcionPersonal = t.description,
            TarifaMaxPorHora = t.totalPrice,
            FranjasHorarias = t.timeOfDay.Count > 0
                ? string.Join(",", t.timeOfDay)
                : string.Empty,
            DiasDisponibles = t.date0.HasValue
                ? t.date0.Value.ToString("dd MMM") +
                  (t.date1.HasValue && t.date1.Value.Date != t.date0.Value.Date
                      ? "," + t.date1.Value.ToString("dd MMM")
                      : "")
                : string.Empty,
            Valoracion = esMio ? _miMedia : 0m,
            NumeroResenas = esMio ? _miTotal : 0,
            NumeroReservas = t.completedTasks,
            Mascotas = t.petList
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(nombre => new MascotaItem
                {
                    Nombre = nombre,
                    TipoAnimal = "Otro"
                })
                .ToList(),
            FotoPerfil = string.IsNullOrEmpty(t.photoPath) ? null : t.photoPath,
        };
    }

    private void ActualizarLista(List<PropietarioItem> lista)
    {
        ListaPropietarios.ItemsSource = lista;
        LblResultados.Text = $"Ver +{lista.Count} resultados";
    }

    private void OnBusquedaChanged(object? sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.ToLower() ?? string.Empty;
        var filtrada = string.IsNullOrWhiteSpace(texto)
            ? _todos
            : _todos.Where(p =>
                p.Nombre.ToLower().Contains(texto) ||
                (p.Localizacion?.ToLower().Contains(texto) ?? false))
              .ToList();
        ActualizarLista(filtrada);
    }

    private async void OnTarjetaTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is PropietarioItem propietario)
            await Navigation.PushModalAsync(new PropietarioPopup(propietario), animated: true);
    }

    private async void OnNuevoPropietarioTapped(object? sender, TappedEventArgs e)
        => await DisplayAlertAsync("Próximamente", "Formulario de nuevo propietario.", "OK");

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

// ── Modelos ───────────────────────────────────────────────────────────────────

public class MascotaItem
{
    public string Nombre { get; set; } = string.Empty;
    public string TipoAnimal { get; set; } = string.Empty;
    public string? Raza { get; set; }
    public int? Edad { get; set; }
    public string? CuidadosEspeciales { get; set; }
    public string? Descripcion { get; set; }

    public bool TieneCuidados => !string.IsNullOrWhiteSpace(CuidadosEspeciales);

    public string Emoji => TipoAnimal.ToLower() switch
    {
        "perro" => "🐕",
        "gato" => "🐈",
        "conejo" => "🐇",
        _ => "🐾"
    };
}

public class PropietarioItem
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
    public decimal TarifaMaxPorHora { get; set; }
    public string? FranjasHorarias { get; set; }
    public string? DiasDisponibles { get; set; }
    public decimal Valoracion { get; set; }
    public int NumeroResenas { get; set; }
    public int NumeroReservas { get; set; }
    public int NumeroLikes { get; set; }
    public List<MascotaItem> Mascotas { get; set; } = new();
    public string? FotoPerfil { get; set; }

    public string Inicial => string.IsNullOrEmpty(Nombre) ? "?" : Nombre[0].ToString().ToUpper();
    public string TarifaTxt => $"Hasta {TarifaMaxPorHora:F2} €/hora";
    public string MascotasTxt => $"{Mascotas.Count} mascota(s)";
    public string ResenasTxt => NumeroResenas == 0 ? "0 reseñas" : $"({NumeroResenas})";

    public List<string> ListaDias =>
        DiasDisponibles?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
    public List<string> ListaFranjas =>
        FranjasHorarias?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();
}