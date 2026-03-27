#pragma warning disable CA1416

using Mascotared.Services;

namespace Mascotared;

public partial class Register : ContentPage
{
    private readonly ApiService _api = new ApiService();
    private bool _esPropietario = true;
    private bool _esCuidador = false;
    private string _genero = "";

    public DateTime EdadMinimaDate => DateTime.Today.AddYears(-18);

    public Register()
    {
        InitializeComponent();
        BindingContext = this;
        // DOB ya no es obligatoria: no preseleccionamos fecha
    }

    // ── Género ────────────────────────────────────────────────────────────
    private void OnHombreTapped(object? sender, TappedEventArgs e)
        => SeleccionarGenero("Hombre");

    private void OnMujerTapped(object? sender, TappedEventArgs e)
        => SeleccionarGenero("Mujer");

    private void OnOtroGeneroTapped(object? sender, TappedEventArgs e)
        => SeleccionarGenero("Otro");

    private void SeleccionarGenero(string genero)
    {
        _genero = genero;
        ActualizarBtnGenero(BtnHombre, genero == "Hombre", "#455AEB");
        ActualizarBtnGenero(BtnMujer, genero == "Mujer", "#455AEB");
        ActualizarBtnGenero(BtnOtroGenero, genero == "Otro", "#455AEB");
    }

    private static void ActualizarBtnGenero(Border btn, bool activo, string color)
    {
        btn.BackgroundColor = activo
            ? Color.FromArgb(color)
            : Colors.White;
        btn.Stroke = new SolidColorBrush(activo
            ? Color.FromArgb(color)
            : Color.FromArgb("#E0E0E0"));
        if (btn.Content is Label lbl)
            lbl.TextColor = activo ? Colors.White : Colors.Gray;
    }

    // ── Rol ───────────────────────────────────────────────────────────────
    private void OnPropietarioTapped(object? sender, TappedEventArgs e)
    {
        _esPropietario = !_esPropietario;
        BtnPropietario.BackgroundColor = _esPropietario
            ? Color.FromArgb("#455AEB") : Colors.White;
        BtnPropietario.Stroke = new SolidColorBrush(_esPropietario
            ? Color.FromArgb("#455AEB") : Color.FromArgb("#E0E0E0"));
        ((Label)BtnPropietario.Content).TextColor =
            _esPropietario ? Colors.White : Colors.Gray;
    }

    private void OnCuidadorTapped(object? sender, TappedEventArgs e)
    {
        _esCuidador = !_esCuidador;
        BtnCuidador.BackgroundColor = _esCuidador
            ? Color.FromArgb("#FE3D7D") : Colors.White;
        BtnCuidador.Stroke = new SolidColorBrush(_esCuidador
            ? Color.FromArgb("#FE3D7D") : Color.FromArgb("#E0E0E0"));
        ((Label)BtnCuidador.Content).TextColor =
            _esCuidador ? Colors.White : Colors.Gray;
    }

    // ── Empresa (próximamente) ─────────────────────────────────────────────
    private async void OnEmpresaTapped(object? sender, TappedEventArgs e)
    {
        await DisplayAlert(
            "Proximamente",
            "El registro para empresas estara disponible muy pronto.",
            "Entendido");
    }

    // ── Registrar ─────────────────────────────────────────────────────────
    private async void OnRegistrarClicked(object? sender, EventArgs e)
    {
        string nombre = NombreEntry.Text?.Trim() ?? "";
        string email = EmailEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";
        string confirmPassword = ConfirmPasswordEntry.Text ?? "";

        if (string.IsNullOrEmpty(nombre) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Rellena todos los campos obligatorios.", "OK");
            return;
        }

        if (password.Length < 8)
        {
            await DisplayAlert("Error", "La contrasena debe tener al menos 8 caracteres.", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Error", "Las contrasenias no coinciden.", "OK");
            return;
        }

        if (!_esPropietario && !_esCuidador)
        {
            await DisplayAlert("Error", "Selecciona al menos un rol.", "OK");
            return;
        }

        try
        {
            // DOB ya no es obligatoria
            bool ok = await _api.RegisterAsync(
    nombre, email, password, _esPropietario, _esCuidador,
    null, string.IsNullOrWhiteSpace(_genero) ? null : _genero);

            if (ok)
            {
                // Ya no guardamos DOB en Preferences
                if (!string.IsNullOrWhiteSpace(_genero))
    Preferences.Set("user_genero", _genero);
else
    Preferences.Remove("user_genero");
                Preferences.Set("user_nombre", nombre);
                Preferences.Set("user_email", email);
                Preferences.Set("user_esPropietario", _esPropietario);
                Preferences.Set("user_esCuidador", _esCuidador);
                Preferences.Set("email_verificado", false);
                Preferences.Set("es_primer_inicio", true);

                await DisplayAlertAsync(
                    "✉️ Verifica tu correo",
                    $"Te hemos enviado un correo de verificación a {email}.\n\nRevisa tu bandeja de entrada y también la carpeta de spam o correo no deseado.\n\nDebes verificar tu cuenta antes de iniciar sesión.",
                    "Entendido");

                await Navigation.PushAsync(new Onboarding());
            }
            else
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo crear la cuenta. El correo puede estar en uso.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de conexion", ex.Message, "OK");
        }
    }

    private async void OnLoginTapped(object? sender, TappedEventArgs e)
        => await Navigation.PopAsync();
}