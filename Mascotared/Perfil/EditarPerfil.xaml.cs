using Mascotared.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Mascotared;

public partial class EditarPerfil : ContentPage
{
    private readonly List<string> _tagsDisponibles = new() { "Propietario/a", "Cuidador/a" };
    private readonly HashSet<string> _tagsSeleccionados = new();

    private readonly List<string> _diasDisponibles = new() { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
    private readonly List<string> _franjasDisponibles = new() { "Mañanas", "Mediodía", "Tardes", "Noches" };
    private readonly HashSet<string> _diasSeleccionados = new();
    private readonly HashSet<string> _franjasSeleccionadas = new();

    private readonly ApiService _api = new();

    // Ruta local de la nueva foto seleccionada (null = no cambió)
    private string? _nuevaFotoRuta = null;
    // URL ya subida al servidor (para no volver a subirla)
    private string? _fotoUrlSubida = null;
    // Coordenadas GPS obtenidas con el botón 📍 (null = no detectadas aún)
    private double? _latitud = null;
    private double? _longitud = null;

    public EditarPerfil()
    {
        InitializeComponent();
        CargarAvatarInicial();
        CargarDesdePreferences();
        _ = CargarDesdeApiAsync();
    }

    // ── Avatar ────────────────────────────────────────────────────────────────

    private void CargarAvatarInicial()
    {
        // Leer foto desde Preferences (guardada en login)
        string foto = Preferences.Get("user_foto_base64", string.Empty);
        if (string.IsNullOrEmpty(foto))
            foto = Preferences.Get("user_foto", string.Empty);

        string nombre = Preferences.Get("user_nombre", "");
        AvatarInicial.Text = nombre.Length > 0 ? nombre[0].ToString().ToUpper() : "?";

        if (!string.IsNullOrEmpty(foto))
            MostrarFotoEnAvatar(foto);
    }

    private void MostrarFotoEnAvatar(string fotoUrlOBase64)
{
    if (fotoUrlOBase64.StartsWith("http://"))
        fotoUrlOBase64 = fotoUrlOBase64.Replace("http://", "https://");

    if (fotoUrlOBase64.StartsWith("https://"))
    {
        AvatarImg.Source = ImageSource.FromUri(new Uri(fotoUrlOBase64));
    }
    else
    {
        var b64 = fotoUrlOBase64.StartsWith("data:")
            ? fotoUrlOBase64[(fotoUrlOBase64.IndexOf(',') + 1)..]
            : fotoUrlOBase64;

        AvatarImg.Source = ImageSource.FromStream(() =>
            new MemoryStream(Convert.FromBase64String(b64)));
    }

    AvatarImg.IsVisible = true;
    AvatarInicial.IsVisible = false;
}

    private async void OnCambiarFotoTapped(object sender, EventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images
            });
            if (resultado == null) return;

            // 1. Preview local inmediato
            AvatarImg.Source = ImageSource.FromFile(resultado.FullPath);
            AvatarImg.IsVisible = true;
            AvatarInicial.IsVisible = false;
            _nuevaFotoRuta = resultado.FullPath;

            // 2. Subir al servidor
            var fotoUrl = await _api.SubirImagenAsync(resultado.FullPath, "perfiles");
            if (fotoUrl == null)
            {
                await DisplayAlertAsync("Aviso", "No se pudo subir la foto al servidor.", "OK");
                return;
            }

            // 3. Guardar en Preferences (ambas claves) y singleton — igual que PerfilConfigUser
            Preferences.Set("user_foto_base64", fotoUrl);
            Preferences.Set("user_foto", fotoUrl);
            UsuarioService.Instancia.Usuario.FotoPerfilBase64 = fotoUrl;

            // 4. Actualizar en la API
            await _api.ActualizarPerfilAsync(new { fotoPerfil = fotoUrl });

            // Guardar la URL para no volver a subirla en OnGuardarClicked
            _nuevaFotoRuta = null;
            _fotoUrlSubida = fotoUrl;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    // ── Carga desde Preferences (instantáneo) ────────────────────────────────

    private void CargarDesdePreferences()
    {
        EntryNombre.Text = Preferences.Get("user_nombre", "");
        EntryLocalizacion.Text = Preferences.Get("user_localizacion", "");
        EntryTarifa.Text = Preferences.Get("user_tarifa", "");
        EditorDescripcion.Text = Preferences.Get("user_descripcion", "");

        long ticks = Preferences.Get("user_fechaNacimiento", 0L);
        if (ticks > 0)
            DatePickerNacimiento.Date = new DateTime(ticks);

        bool esPropietario = Preferences.Get("user_esPropietario", false);
        bool esCuidador = Preferences.Get("user_esCuidador", false);
        if (esPropietario) _tagsSeleccionados.Add("Propietario/a");
        if (esCuidador) _tagsSeleccionados.Add("Cuidador/a");

        string dias = Preferences.Get("user_dias", "");
        string franjas = Preferences.Get("user_franjas", "");
        if (!string.IsNullOrEmpty(dias))
            foreach (var d in dias.Split(',', StringSplitOptions.RemoveEmptyEntries))
                _diasSeleccionados.Add(d);
        if (!string.IsNullOrEmpty(franjas))
            foreach (var f in franjas.Split(',', StringSplitOptions.RemoveEmptyEntries))
                _franjasSeleccionadas.Add(f);

        GenerarChipsToggle(FlexTags, _tagsDisponibles, _tagsSeleccionados, OnTagToggled);
        GenerarChipsToggle(FlexDiasEdit, _diasDisponibles, _diasSeleccionados, OnDiaToggled);
        GenerarChipsToggle(FlexFranjasEdit, _franjasDisponibles, _franjasSeleccionadas, OnFranjaToggled);
    }

    // ── Carga fresca desde la API (fuente de verdad) ──────────────────────────

    private async Task CargarDesdeApiAsync()
    {
        try
        {
            var perfil = await _api.GetPerfilAsync();
            if (perfil == null) return;

            // Nombre y datos de texto
            EntryNombre.Text = perfil.NombreCompleto ?? EntryNombre.Text;
            EntryLocalizacion.Text = perfil.Direccion ?? EntryLocalizacion.Text;
            EntryTarifa.Text = perfil.TarifaPorHora.HasValue
                ? perfil.TarifaPorHora.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : EntryTarifa.Text;
            EditorDescripcion.Text = perfil.Bio ?? EditorDescripcion.Text;

            // Fecha de nacimiento — fuente de verdad es la BD
            if (perfil.FechaNacimiento.HasValue)
                DatePickerNacimiento.Date = perfil.FechaNacimiento.Value;

            // Tags rol
            _tagsSeleccionados.Clear();
            if (perfil.EsPropietario) _tagsSeleccionados.Add("Propietario/a");
            if (perfil.EsCuidador) _tagsSeleccionados.Add("Cuidador/a");

            // Días y franjas
            _diasSeleccionados.Clear();
            _franjasSeleccionadas.Clear();
            if (!string.IsNullOrEmpty(perfil.DiasDisponibles))
                foreach (var d in perfil.DiasDisponibles.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    _diasSeleccionados.Add(d);
            if (!string.IsNullOrEmpty(perfil.FranjasHorarias))
                foreach (var f in perfil.FranjasHorarias.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    _franjasSeleccionadas.Add(f);

            // Refrescar chips con datos de la API
            GenerarChipsToggle(FlexTags, _tagsDisponibles, _tagsSeleccionados, OnTagToggled);
            GenerarChipsToggle(FlexDiasEdit, _diasDisponibles, _diasSeleccionados, OnDiaToggled);
            GenerarChipsToggle(FlexFranjasEdit, _franjasDisponibles, _franjasSeleccionadas, OnFranjaToggled);

            // Cargar coordenadas actuales (para no perderlas al guardar sin usar GPS)
            if (perfil.Latitud.HasValue) _latitud = perfil.Latitud.Value;
            if (perfil.Longitud.HasValue) _longitud = perfil.Longitud.Value;

            // Actualizar Preferences con los datos reales para que PerfilConfigUser los vea
            SincronizarPreferences(perfil);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EditarPerfil] Error cargando API: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SincronizarPreferences(PerfilDto p)
    {
        if (!string.IsNullOrEmpty(p.NombreCompleto))
            Preferences.Set("user_nombre", p.NombreCompleto);
        if (!string.IsNullOrEmpty(p.Direccion))
            Preferences.Set("user_localizacion", p.Direccion);
        if (!string.IsNullOrEmpty(p.Bio))
            Preferences.Set("user_descripcion", p.Bio);
        if (p.TarifaPorHora.HasValue)
            Preferences.Set("user_tarifa",
                p.TarifaPorHora.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (p.FechaNacimiento.HasValue)
            Preferences.Set("user_fechaNacimiento", p.FechaNacimiento.Value.Ticks);
        Preferences.Set("user_esPropietario", p.EsPropietario);
        Preferences.Set("user_esCuidador", p.EsCuidador);
        if (!string.IsNullOrEmpty(p.DiasDisponibles))
            Preferences.Set("user_dias", p.DiasDisponibles);
        if (!string.IsNullOrEmpty(p.FranjasHorarias))
            Preferences.Set("user_franjas", p.FranjasHorarias);
        if (!string.IsNullOrEmpty(p.Idioma))
            Preferences.Set("user_idioma", p.Idioma);
    }

    private static void GenerarChipsToggle(
        FlexLayout flex,
        List<string> opciones,
        HashSet<string> seleccionados,
        EventHandler<string> onToggle)
    {
        flex.Children.Clear();
        foreach (var opcion in opciones)
        {
            bool activo = seleccionados.Contains(opcion);
            var chip = CrearChipToggle(opcion, activo);
            var tap = new TapGestureRecognizer();
            var captura = opcion;
            tap.Tapped += (s, e) => onToggle(s!, captura);
            chip.GestureRecognizers.Add(tap);
            flex.Children.Add(chip);
        }
    }

    private static Border CrearChipToggle(string texto, bool activo) => new()
    {
        BackgroundColor = activo ? Color.FromArgb("#455AEB") : Color.FromArgb("#F4F6FA"),
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
        Stroke = activo ? Colors.Transparent : Color.FromArgb("#D0D5EE"),
        StrokeThickness = activo ? 0 : 1,
        Padding = new Thickness(14, 7),
        Margin = new Thickness(0, 0, 8, 8),
        Content = new Label
        {
            Text = texto,
            TextColor = activo ? Colors.White : Color.FromArgb("#455AEB"),
            FontFamily = "OpenSansSemibold",
            FontSize = 13
        }
    };

    private void RefrescarChips(FlexLayout flex, List<string> opciones, HashSet<string> seleccionados, EventHandler<string> onToggle)
        => GenerarChipsToggle(flex, opciones, seleccionados, onToggle);

    private void OnTagToggled(object? sender, string tag)
    {
        if (_tagsSeleccionados.Contains(tag)) _tagsSeleccionados.Remove(tag);
        else _tagsSeleccionados.Add(tag);
        RefrescarChips(FlexTags, _tagsDisponibles, _tagsSeleccionados, OnTagToggled);
    }

    private void OnDiaToggled(object? sender, string dia)
    {
        if (_diasSeleccionados.Contains(dia)) _diasSeleccionados.Remove(dia);
        else _diasSeleccionados.Add(dia);
        RefrescarChips(FlexDiasEdit, _diasDisponibles, _diasSeleccionados, OnDiaToggled);
    }

    private void OnFranjaToggled(object? sender, string franja)
    {
        if (_franjasSeleccionadas.Contains(franja)) _franjasSeleccionadas.Remove(franja);
        else _franjasSeleccionadas.Add(franja);
        RefrescarChips(FlexFranjasEdit, _franjasDisponibles, _franjasSeleccionadas, OnFranjaToggled);
    }

    // ── Guardar ───────────────────────────────────────────────────────────────

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryNombre.Text))
        {
            await DisplayAlertAsync("Error", "El nombre no puede estar vacío.", "OK");
            return;
        }

        if (!decimal.TryParse(EntryTarifa.Text?.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal tarifa))
        {
            tarifa = 0;
        }

        // Fecha de nacimiento — en algunas versiones de MAUI Date es nullable
        DateTime fechaNac = DatePickerNacimiento.Date ?? DateTime.Today;

        string diasStr = string.Join(",", _diasSeleccionados);
        string franjasStr = string.Join(",", _franjasSeleccionadas);
        bool esPropietario = _tagsSeleccionados.Contains("Propietario/a");
        bool esCuidador = _tagsSeleccionados.Contains("Cuidador/a");
        string nombre = EntryNombre.Text.Trim();
        string localizacion = EntryLocalizacion.Text?.Trim() ?? "";
        string descripcion = EditorDescripcion.Text?.Trim() ?? "";

        // ── 1. Foto — ya se subió en OnCambiarFotoTapped, solo pasamos la URL ──
        string? nuevaFotoUrl = _fotoUrlSubida; // null si no cambió

        // ── 2. Guardar en la API ──────────────────────────────────────────────
        bool ok = await _api.ActualizarPerfilAsync(new
        {
            nombreCompleto = nombre,
            bio = descripcion,
            tarifaPorHora = tarifa > 0 ? tarifa : (decimal?)null,
            diasDisponibles = diasStr,
            franjasHorarias = franjasStr,
            direccion = localizacion,
            fechaNacimiento = fechaNac,
            esPropietario,
            esCuidador,
            fotoPerfil = nuevaFotoUrl  // null si no cambió → la API no la toca
        });

        if (!ok)
        {
            await DisplayAlertAsync("Error", "No se pudo guardar en el servidor. Comprueba tu conexión.", "OK");
            return;
        }

        // Sincronizar coordenadas GPS si están disponibles
        if (_latitud.HasValue && _longitud.HasValue)
            await _api.ActualizarUbicacionAsync(_latitud.Value, _longitud.Value, localizacion);

        // ── 3. Actualizar Preferences ─────────────────────────────────────────
        Preferences.Set("user_nombre", nombre);
        Preferences.Set("user_localizacion", localizacion);
        Preferences.Set("user_descripcion", descripcion);
        Preferences.Set("user_tarifa", tarifa.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Preferences.Set("user_dias", diasStr);
        Preferences.Set("user_franjas", franjasStr);
        Preferences.Set("user_fechaNacimiento", fechaNac.Ticks);
        Preferences.Set("user_esPropietario", esPropietario);
        Preferences.Set("user_esCuidador", esCuidador);

        // Si cambió la foto, actualizar en Preferences (ambas claves) y en el singleton
        if (nuevaFotoUrl != null)
        {
            Preferences.Set("user_foto", nuevaFotoUrl);
            Preferences.Set("user_foto_base64", nuevaFotoUrl);
            UsuarioService.Instancia.Usuario.FotoPerfilBase64 = nuevaFotoUrl;
        }

        // ── 4. Actualizar UsuarioService en memoria ───────────────────────────
        var u = UsuarioService.Instancia.Usuario;
        u.Nombre = nombre;
        u.Localizacion = localizacion;
        u.DescripcionPersonal = descripcion;
        u.Tags = _tagsSeleccionados.ToList();
        u.DiasDisponibles = diasStr;
        u.FranjasHorarias = franjasStr;

        await DisplayAlertAsync("Guardado", "Tu perfil se ha actualizado correctamente.", "OK");
        await Navigation.PopAsync(animated: true);
    }

    // ── Detectar ubicación GPS ────────────────────────────────────────────────

    private async void OnDetectarUbicacionTapped(object sender, EventArgs e)
    {
        LabelGpsBtn.Text = "...";
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                LabelGpsBtn.Text = "GPS";
                await DisplayAlertAsync("Permisos", "Necesitas conceder permiso de ubicación.", "OK");
                return;
            }

            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });

            if (loc == null) { LabelGpsBtn.Text = "GPS"; return; }

            _latitud = loc.Latitude;
            _longitud = loc.Longitude;

            // Geocodificación inversa → texto legible
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(loc.Latitude, loc.Longitude);
            var place = placemarks?.FirstOrDefault();
            if (place != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(place.SubLocality)) parts.Add(place.SubLocality);
                if (!string.IsNullOrEmpty(place.Locality)) parts.Add(place.Locality);
                if (!string.IsNullOrEmpty(place.AdminArea)) parts.Add(place.AdminArea);
                if (parts.Count > 0)
                    EntryLocalizacion.Text = string.Join(", ", parts);
            }

            LabelGpsBtn.Text = "OK";
        }
        catch
        {
            LabelGpsBtn.Text = "GPS";
            await DisplayAlertAsync("Error", "No se pudo obtener la ubicación.", "OK");
        }
    }

    private async void OnCancelarClicked(object? sender, EventArgs e)
        => await Navigation.PopAsync(animated: true);
}