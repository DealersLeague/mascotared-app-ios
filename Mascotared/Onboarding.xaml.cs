namespace Mascotared;

public partial class Onboarding : ContentPage
{
    private int _indiceActual = 0;

    public List<OnboardingSlide> Slides { get; } = new()
    {
        new OnboardingSlide
        {
            Emoji = "🐾",
            Titulo = "Bienvenido a MascotaRed",
            Bullets = new()
            {
                "Tu red de confianza para mascotas",
                "Conectamos propietarios con cuidadores verificados cerca de ti",
                "Perros, gatos, conejos, aves, reptiles, caballos y más"
            }
        },
        new OnboardingSlide
        {
            Emoji = "🔍",
            Titulo = "Encuentra cuidadores",
            Bullets = new()
            {
                "Busca cuidadores por ubicación, disponibilidad y tipo de mascota",
                "Los cuidadores se promocionan: tú eliges y los contactas directamente",
                "Filtra por servicio: paseos, guardería en casa, visitas a domicilio"
            }
        },
        new OnboardingSlide
        {
            Emoji = "📋",
            Titulo = "Publica tu solicitud",
            Bullets = new()
            {
                "¿Necesitas cuidador urgente? Publica " +
                "una solicitud de cuidado",
                "Los cuidadores disponibles se postulan y " +
                "tú decides con quién hablar",
                "Sin intermediarios — acordáis los detalles directamente"
            }
        },
        new OnboardingSlide
        {
            Emoji = "🐶",
            Titulo = "El perfil de tu mascota",
            Bullets = new()
            {
                "Añade información de salud, " +
                "comportamiento y rutinas diarias",
                "Incluye protocolo de emergencia" +
                " y contacto veterinario",
                "El cuidador llega siempre preparado y sin sorpresas"
            }
        },
        new OnboardingSlide
        {
            Emoji = "🏢",
            Titulo = "¿Empresa del sector animal?",
            Bullets = new()
            {
                "Farmacias, veterinarias y tiendas de " +
                "animales pueden registrarse",
                "Peluquerías caninas, veterinarios exóticos y " +
                "centros especializados",
                "Promociona tu negocio y llega a toda " +
                "la comunidad de MascotaRed"
            }
        },
        new OnboardingSlide
        {
            Emoji = "💬",
            Titulo = "Conecta y gestiona",
            Bullets = new()
            {
                "Mensajes directos entre propietario y cuidador, sin comisiones",
                "Consulta el historial de cuidados y valoraciones reales",
                "Recibe actualizaciones y fotos de tu mascota en tiempo real",
                "Sube y presume a tu mascota en nuestra red"
            }
        }
    };

    public Onboarding()
    {
        InitializeComponent();
        BindingContext = this;
        CarouselSlides.IndicatorView = Indicadores;
    }

    private void OnSlideChanged(object sender, CurrentItemChangedEventArgs e)
    {
        if (CarouselSlides.CurrentItem is not OnboardingSlide) return;

        _indiceActual = Slides.IndexOf((OnboardingSlide)CarouselSlides.CurrentItem);
        bool esUltimo = _indiceActual == Slides.Count - 1;

        BtnSiguiente.Text = esUltimo ? "¡Empezar! 🚀" : "Continuar";

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        BtnSiguiente.BackgroundColor = esUltimo
            ? Color.FromArgb(isDark ? "#FF4F8F" : "#FE3D7D")
            : Color.FromArgb(isDark ? "#5C6CFF" : "#455AEB");
    }

    private async void OnSiguienteClicked(object sender, EventArgs e)
    {
        if (_indiceActual < Slides.Count - 1)
            CarouselSlides.ScrollTo(_indiceActual + 1);
        else
            await IrAlLogin();
    }

    private static async Task IrAlLogin()
    {
        Preferences.Set("es_primer_inicio", false);
        Application.Current!.Dispatcher.Dispatch(() =>
        {
            Application.Current.MainPage = new NavigationPage(new LogIn());
        });
        await Task.CompletedTask;
    }
}

public class OnboardingSlide
{
    public string Emoji { get; set; } = "";
    public string Titulo { get; set; } = "";
    public List<string> Bullets { get; set; } = new();
}