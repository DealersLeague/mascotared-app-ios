#pragma warning disable CA1416
using Mascotared.Services;
using Mascotared.Models;
namespace Mascotared;

public partial class LogIn : ContentPage
{
    private readonly ApiService _api = new ApiService();
    public LogIn()
    {
        InitializeComponent();
    }
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlertAsync("Error", "Introduce correo y contraseña", "OK");
            return;
        }
        try
        {
            var result = await _api.LoginAsync(email, password);
            if (result != null)
            {
                // Limpiar datos de la sesión anterior antes de cargar la nueva
                UsuarioService.Instancia.Reset();
                Preferences.Remove("user_foto_base64");
                Preferences.Remove("user_localizacion");

                Preferences.Set("jwt_token", result.Token);
                Preferences.Set("user_id", result.UserId);
                Preferences.Set("user_nombre", result.NombreCompleto);
                Preferences.Set("user_email", result.Email);
                Preferences.Set("user_esPropietario", result.EsPropietario);
                Preferences.Set("user_esCuidador", result.EsCuidador);
                Preferences.Set("user_foto", result.FotoPerfil ?? "");
                Preferences.Set("user_foto_base64", result.FotoPerfil ?? "");

                await CargarDatosEnSingletonAsync(result);

                Application.Current!.Dispatcher.Dispatch(() =>
                {
                    Application.Current!.Windows[0].Page = new AppShell();
                });
            }
            else
            {
                await DisplayAlertAsync("Error", "Correo o contraseña incorrectos", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error de conexión", ex.Message, "OK");
        }
    }

    private async Task CargarDatosEnSingletonAsync(LoginResult result)
    {
        try
        {
            var perfil = await _api.GetPerfilAsync();

            var usuario = new UsuarioItem
            {
                Nombre = perfil?.NombreCompleto ?? result.NombreCompleto ?? "",
                DescripcionPersonal = perfil?.Bio ?? "",
                Localizacion = perfil?.Direccion ?? "",
                TarifaPorHora = perfil?.TarifaPorHora ?? 0,
                DiasDisponibles = perfil?.DiasDisponibles ?? "",
                FranjasHorarias = perfil?.FranjasHorarias ?? "",
                FotoPerfilBase64 = perfil?.FotoPerfil ?? result.FotoPerfil ?? "",
                Idioma = perfil?.Idioma ?? "Español",
                Tema = "Claro",
                TamanoLetra = "Mediano",
                Verificado = false,
                Tags = new List<string>(),
                Mascotas = new List<MascotaItem>()
            };

            if (!string.IsNullOrEmpty(usuario.FotoPerfilBase64))
                Preferences.Set("user_foto_base64", usuario.FotoPerfilBase64);
            if (!string.IsNullOrEmpty(perfil?.Direccion))
                Preferences.Set("user_localizacion", perfil.Direccion);

            var mascotas = await _api.GetMascotasAsync();
            foreach (var m in mascotas)
            {
                string nombre = m.TryGetProperty("nombre", out var n) ? n.GetString() ?? "" : "";
                string especie = m.TryGetProperty("tipoAnimal", out var t) ? t.GetString() ?? "" : "";
                string raza = m.TryGetProperty("raza", out var r) ? r.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(nombre))
                    usuario.Mascotas.Add(new MascotaItem { Nombre = nombre, TipoAnimal = especie, Raza = raza });
            }

            var mascotasJson = System.Text.Json.JsonSerializer.Serialize(
                usuario.Mascotas.Select(m => new { m.Nombre, m.TipoAnimal, m.Raza }));
            Preferences.Set("user_mascotas_json", mascotasJson);

            UsuarioService.Instancia.ActualizarUsuario(usuario);
        }
        catch
        {
            var nombre = Preferences.Get("user_nombre", result.NombreCompleto ?? "");
            var foto = Preferences.Get("user_foto_base64", result.FotoPerfil ?? "");

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
                Idioma = Preferences.Get("user_idioma", "Español"),
                Tema = Preferences.Get("user_tema", "Claro"),
                TamanoLetra = Preferences.Get("user_letra", "Mediano"),
                DescripcionPersonal = Preferences.Get("user_descripcion", ""),
                Localizacion = Preferences.Get("user_localizacion", ""),
                Tags = new List<string>(),
                Mascotas = mascotas
            });
        }
    }

    // ── ¿Olvidaste tu contraseña? ─────────────────────────────────────────────
    private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(email))
        {
            await DisplayAlertAsync("Introduce tu correo",
                "Escribe tu correo electrónico en el campo de arriba y pulsa aquí.", "OK");
            return;
        }

        bool ok = await _api.ForgotPasswordAsync(email);
        if (ok)
            await DisplayAlertAsync("Correo enviado",
                "Si el correo existe recibirás un enlace para restablecer tu contraseña.", "OK");
        else
            await DisplayAlertAsync("Error", "No se pudo enviar el correo. Inténtalo de nuevo.", "OK");
    }

    private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            var googleAuth = new GoogleAuthService();
            var idToken = await googleAuth.ObtenerIdTokenAsync();

            if (string.IsNullOrEmpty(idToken))
            {
                await DisplayAlertAsync("Error", "No se pudo obtener el token de Google", "OK");
                return;
            }

            var result = await _api.LoginConGoogleAsync(idToken);
            if (result != null)
            {
                UsuarioService.Instancia.Reset();
                Preferences.Remove("user_foto_base64");
                Preferences.Remove("user_localizacion");

                Preferences.Set("jwt_token", result.Token);
                Preferences.Set("user_id", result.UserId);
                Preferences.Set("user_nombre", result.NombreCompleto);
                Preferences.Set("user_email", result.Email);
                Preferences.Set("user_esPropietario", result.EsPropietario);
                Preferences.Set("user_esCuidador", result.EsCuidador);
                Preferences.Set("user_foto", result.FotoPerfil ?? "");
                Preferences.Set("user_foto_base64", result.FotoPerfil ?? "");

                await CargarDatosEnSingletonAsync(result);

                if (result.EsNuevo)
                {
                    Preferences.Set("es_primer_inicio", true);
                    await Navigation.PushAsync(new Onboarding());
                }
                else
                {
                    Application.Current!.Dispatcher.Dispatch(() =>
                    {
                        Application.Current!.Windows[0].Page = new AppShell();
                    });
                }
            }
            else
            {
                await DisplayAlertAsync("Error", "No se pudo iniciar sesión con Google", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new Register());
    }

    private void OnExplorarClicked(object sender, EventArgs e)
    {
        Application.Current!.Dispatcher.Dispatch(() =>
        {
            Application.Current!.Windows[0].Page = new AppShell();
        });
    }
}