using Mascotared.Services;
using Mascotared.Models;
namespace Mascotared
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStart()
        {
            base.OnStart();
            OneSignalService.Inicializar();
            _ = ValidarTokenAsync();
        }

        private static async Task ValidarTokenAsync()
        {
            var token = Preferences.Get("jwt_token", "");
            if (string.IsNullOrEmpty(token)) return;

            try
            {
                var api = new ApiService();
                var perfil = await api.GetPerfilAsync();
                if (perfil == null)
                {
                    // Token expirado o inválido → borrar sesión y volver al login
                    Preferences.Remove("jwt_token");
                    Preferences.Remove("user_id");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (Application.Current?.Windows.Count > 0)
                            Application.Current.Windows[0].Page =
                                new NavigationPage(new LogIn());
                    });
                }
            }
            catch { }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var token = Preferences.Get("jwt_token", "");
            var userId = Preferences.Get("user_id", "");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                RestaurarSesionDesdeCaché();
                return new Window(new AppShell());
            }

            return new Window(new NavigationPage(new LogIn()));
        }

        private static void RestaurarSesionDesdeCaché()
        {
            try
            {
                var nombre = Preferences.Get("user_nombre", "");
                var foto = Preferences.Get("user_foto_base64", "");
                var localizacion = Preferences.Get("user_localizacion", "");

                var mascotas = new List<MascotaItem>();
                var mascotasJson = Preferences.Get("user_mascotas_json", "");
                if (!string.IsNullOrEmpty(mascotasJson))
                {
                    try
                    {
                        var lista = System.Text.Json.JsonSerializer.Deserialize<
                            List<System.Text.Json.JsonElement>>(mascotasJson);
                        if (lista != null)
                            foreach (var m in lista)
                            {
                                string n = m.TryGetProperty("Nombre", out var pn) ? pn.GetString() ?? "" : "";
                                string t = m.TryGetProperty("TipoAnimal", out var pt) ? pt.GetString() ?? "" : "";
                                string r = m.TryGetProperty("Raza", out var pr) ? pr.GetString() ?? "" : "";
                                if (!string.IsNullOrEmpty(n))
                                    mascotas.Add(new MascotaItem { Nombre = n, TipoAnimal = t, Raza = r });
                            }
                    }
                    catch { }
                }

                UsuarioService.Instancia.ActualizarUsuario(new UsuarioItem
                {
                    Nombre = nombre,
                    FotoPerfilBase64 = foto,
                    Localizacion = localizacion,
                    Idioma = Preferences.Get("user_idioma", "Español"),
                    Tema = Preferences.Get("user_tema", "Claro"),
                    TamanoLetra = Preferences.Get("user_letra", "Mediano"),
                    DescripcionPersonal = Preferences.Get("user_descripcion", ""),
                    Tags = new List<string>(),
                    Mascotas = mascotas
                });
            }
            catch { }
        }
    }
}
