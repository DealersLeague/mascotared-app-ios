using Mascotared.Models;
using Mascotared.Services;
using System.Collections.ObjectModel;

namespace Mascotared.Views;

public partial class MisMascotasLista : ContentPage
{
    private readonly ObservableCollection<MascotasItem> _todas = new();
    private readonly ObservableCollection<MascotasItem> _filtradas = new();
    private string _filtroActivo = "Todos";
    private MascotasItem? _mascotaPopup;
    private readonly ApiService _api = new();
    private static bool _sincronizacionHecha = false;  // evita subir duplicados en cada OnAppearing

    public MisMascotasLista()
    {
        InitializeComponent();
        GridMascotas.ItemsSource = _filtradas;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Asigna al usuario actual las mascotas guardadas antes del fix (una sola vez)
        await MigrarMascotasSinUsuarioAsync();

        // Sube al backend las mascotas sin ApiId (solo una vez por sesión para evitar duplicados)
        if (!_sincronizacionHecha)
        {
            _sincronizacionHecha = true;
            await SincronizarMascotasSinApiIdAsync();
        }

        await CargarMascotasAsync();
        AplicarFiltro(_filtroActivo);

        try
        {
            var convs = await _api.GetConversacionesAsync();
            BadgeNoLeidos.IsVisible = convs.Any(j =>
                j.TryGetProperty("noLeidos", out var nl) && nl.GetInt32() > 0);
        }
        catch { }
    }

    // ── Migración única: mascotas sin UsuarioId → usuario activo ─────────
    private static async Task MigrarMascotasSinUsuarioAsync()
    {
        var miId = Preferences.Get("user_id", string.Empty);
        if (string.IsNullOrEmpty(miId)) return;

        var todas = await MascotaRepository.Instance.GetAllAsync();
        var huerfanas = todas.Where(m => string.IsNullOrEmpty(m.UsuarioId)).ToList();
        if (huerfanas.Count == 0) return;

        foreach (var m in huerfanas)
        {
            m.UsuarioId = miId;
            await MascotaRepository.Instance.SaveOrUpdateAsync(m);
        }
    }

    // ── Sincronizar mascotas locales sin ApiId con el backend ─────────────
    private async Task SincronizarMascotasSinApiIdAsync()
    {
        try
        {
            var miId = Preferences.Get("user_id", string.Empty);
            if (string.IsNullOrEmpty(miId)) return;

            var todas = await MascotaRepository.Instance.GetAllAsync();
            var sinSubir = todas.Where(m => m.UsuarioId == miId && m.ApiId == 0).ToList();
            if (sinSubir.Count == 0) return;

            foreach (var m in sinSubir)
            {
                var dto = new
                {
                    Nombre = m.Name,
                    Especie = m.Especie,
                    Raza = m.Race,
                    FechaNacimiento = m.FechaNacimiento,
                    Descripcion = m.Description,
                    CuidadosEspeciales = m.CuidadosEspeciales,
                };

                string? fotoLocal = string.IsNullOrEmpty(m.PhotoPath) ? null : m.PhotoPath;
                int newApiId = await _api.CrearMascotaAsync(dto, fotoLocal);
                if (newApiId > 0)
                {
                    m.ApiId = newApiId;
                    await MascotaRepository.Instance.SaveOrUpdateAsync(m);
                }
            }
        }
        catch { /* No bloquear el arranque si la API falla */ }
    }

    // ── Carga solo las mascotas del usuario activo ────────────────────────
    private async Task CargarMascotasAsync()
    {
        var miId = Preferences.Get("user_id", string.Empty);
        var lista = await MascotaRepository.Instance.GetAllAsync();
        _todas.Clear();
        foreach (var m in lista.Where(m => m.UsuarioId == miId))
            _todas.Add(m);
    }

    // ── Filtros ───────────────────────────────────────────────────────────
    private void OnFiltroTapped(object sender, EventArgs e)
    {
        if (sender is not Border chip) return;
        var tap = (TapGestureRecognizer)chip.GestureRecognizers[0];
        _filtroActivo = tap.CommandParameter?.ToString() ?? "Todos";
        AplicarFiltro(_filtroActivo);
    }

    private void AplicarFiltro(string filtro)
    {
        var chips = new Dictionary<string, Border>
        {
            { "Todos", ChipTodos }, { "Perro", ChipPerro },
            { "Gato",  ChipGato  }, { "Ave",   ChipAve  },
            { "Otros", ChipOtros }
        };
        foreach (var (key, chip) in chips)
        {
            bool activo = key == filtro;
            chip.BackgroundColor = activo ? Color.FromArgb("#455AEB") : Colors.White;
            chip.Stroke = activo ? Colors.Transparent : Color.FromArgb("#EEF0FB");
            if (chip.Content is Label lbl)
                lbl.TextColor = activo ? Colors.White : Color.FromArgb("#9AA0C4");
        }
        _filtradas.Clear();
        var resultado = filtro == "Todos"
            ? (IEnumerable<MascotasItem>)_todas
            : _todas.Where(m =>
                filtro == "Otros"
                    ? m.Especie is not ("Perro" or "Gato" or "Ave")
                    : m.Especie == filtro);
        foreach (var m in resultado) _filtradas.Add(m);
        EstadoVacio.IsVisible = _filtradas.Count == 0;
        GridMascotas.IsVisible = _filtradas.Count > 0;
    }

    // ── Tap tarjeta → popup ───────────────────────────────────────────────
    private void OnMascotaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MascotasItem mascota) return;
        GridMascotas.SelectedItem = null;
        MostrarPopup(mascota);
    }

    private void MostrarPopup(MascotasItem m)
    {
        _mascotaPopup = m;

        PopupFoto.Source = string.IsNullOrEmpty(m.PhotoPath)
            ? "placeholder_pet.png"
            : m.PhotoPath.StartsWith("http")
                ? ImageSource.FromUri(new Uri(m.PhotoPath))
                : ImageSource.FromFile(m.PhotoPath);

        PopupNombre.Text = m.Name;
        PopupEspecie.Text = BuildEspecieLinea(m);

        // Pills principales
        PopupPills.Children.Clear();
        AddPill(PopupPills, m.Race, "#F0F1FD", "#455AEB");
        AddPill(PopupPills, m.Sexo, "#FFF0F5", "#FE3D7D");
        if (m.Peso.HasValue)
            AddPill(PopupPills, $"{m.Peso} {m.UnidadPeso}", "#F5FFF0", "#22AA55");

        // Tags personalidad
        PopupTagsPersonalidad.Children.Clear();
        if (!string.IsNullOrEmpty(m.Tags))
            foreach (var tag in m.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                AddPill(PopupTagsPersonalidad, tag, "#EEF0FB", "#455AEB");

        // Información básica
        PopupFechaNac.Text = m.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "—";
        PopupEdad.Text = BuildEdadTexto(m);
        PopupReproductivo.Text = m.EstadoReproductivo ?? "—";
        PopupColor.Text = string.IsNullOrEmpty(m.ColorMarcas) ? "—" : m.ColorMarcas;

        // Salud y necesidades
        bool tieneAlergias = !string.IsNullOrEmpty(m.Alergias);
        SecAlergias.IsVisible = tieneAlergias;
        PopupAlergias.IsVisible = tieneAlergias;
        PopupAlergias.Text = m.Alergias ?? "";

        PopupCondicionesPills.Children.Clear();
        bool tieneCondiciones = !string.IsNullOrEmpty(m.CondicionesMedicas)
                                && m.CondicionesMedicas != "Ninguna";
        SecCondiciones.IsVisible = tieneCondiciones;
        PopupCondicionesPills.IsVisible = tieneCondiciones;
        if (tieneCondiciones)
            foreach (var c in m.CondicionesMedicas!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                AddPill(PopupCondicionesPills, c, "#FFEEF4", "#FE3D7D");

        bool tieneMovilidad = !string.IsNullOrEmpty(m.NivelMovilidad) && m.NivelMovilidad != "Normal";
        SecMovilidad.IsVisible = tieneMovilidad;
        PopupMovilidad.IsVisible = tieneMovilidad;
        PopupMovilidad.Text = m.NivelMovilidad ?? "";

        bool tieneAlimProhibidos = !string.IsNullOrEmpty(m.AlimentosProhibidos);
        SecAlimProhibidos.IsVisible = tieneAlimProhibidos;
        PopupAlimProhibidos.IsVisible = tieneAlimProhibidos;
        PopupAlimProhibidos.Text = m.AlimentosProhibidos ?? "";

        // Veterinario
        bool tieneVet = !string.IsNullOrEmpty(m.VeterinarioNombre);
        SecVetHeader.IsVisible = tieneVet;
        PopupVetNombre.IsVisible = tieneVet;
        PopupVetDir.IsVisible = tieneVet;
        PopupVetTel.IsVisible = tieneVet;
        PopupVetNombre.Text = m.VeterinarioNombre ?? "";
        PopupVetDir.Text = m.VeterinarioDireccion ?? "";
        PopupVetTel.Text = m.VeterinarioTelefono ?? "";

        // Comportamiento
        PopupMiedos.Text = string.IsNullOrEmpty(m.MiedosReactividad) ? "—" : m.MiedosReactividad;
        PopupCorrea.Text = string.IsNullOrEmpty(m.TipoCorreaArnes) ? "—" : m.TipoCorreaArnes;

        var social = new List<string>();
        if (m.InstintoPresa == true) social.Add("⚠️ Instinto de presa");
        if (m.BienConNinos == true) social.Add("niños ✅");
        if (m.BienConMismaEspecie == true) social.Add("misma especie ✅");
        if (m.BienConOtrasEspecies == true) social.Add("otras especies ✅");
        PopupSocial.Text = social.Count > 0 ? string.Join("  ·  ", social) : "—";

        // Cuidados
        PopupEnergia.Text = m.NivelEnergia ?? "—";
        PopupAlim.Text = string.IsNullOrEmpty(m.FrecuenciaAlimentacion) ? "—" : m.FrecuenciaAlimentacion;
        string med = m.TipoMedicacion ?? "Ninguna";
        if (!string.IsNullOrEmpty(m.HorarioMedicacion)) med += $" · {m.HorarioMedicacion}";
        PopupMed.Text = med;

        // Emergencia
        bool tieneEmergencia = !string.IsNullOrEmpty(m.CentroUrgencias)
                               || !string.IsNullOrEmpty(m.TelefonoUrgencias);
        SecEmergencia.IsVisible = tieneEmergencia;
        PopupUrgencias.IsVisible = tieneEmergencia;
        PopupTelUrgencias.IsVisible = tieneEmergencia;
        PopupPresupuesto.IsVisible = tieneEmergencia;
        PopupUrgencias.Text = m.CentroUrgencias ?? "—";
        PopupTelUrgencias.Text = m.TelefonoUrgencias ?? "—";
        PopupPresupuesto.Text = m.PresupuestoEmergencia ?? "—";

        // Notas
        PopupNotas.Text = string.IsNullOrEmpty(m.Description) ? "Sin notas adicionales." : m.Description;

        // Animación
        PopupDetalle.TranslationY = 600;
        PopupDetalle.IsVisible = true;
        DimOverlay.IsVisible = true;
        PopupDetalle.TranslateToAsync(0, 0, 280, Easing.CubicOut);
        DimOverlay.FadeTo(1, 220);
    }

    private void AddPill(FlexLayout flex, string? text, string bg, string fg)
    {
        if (string.IsNullOrEmpty(text)) return;
        flex.Children.Add(new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            BackgroundColor = Color.FromArgb(bg),
            Stroke = Colors.Transparent,
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 6, 6),
            Content = new Label
            {
                Text = text,
                FontSize = 12,
                TextColor = Color.FromArgb(fg),
                FontFamily = "InterSemiBold"
            }
        });
    }

    private async void OnCerrarPopupTapped(object sender, EventArgs e)
    {
        await Task.WhenAll(
            PopupDetalle.TranslateToAsync(0, 600, 240, Easing.CubicIn),
            DimOverlay.FadeTo(0, 200)
        );
        PopupDetalle.IsVisible = false;
        DimOverlay.IsVisible = false;
        _mascotaPopup = null;
    }

    private async Task CerrarPopupAsync()
    {
        await Task.WhenAll(
            PopupDetalle.TranslateTo(0, 600, 240, Easing.CubicIn),
            DimOverlay.FadeTo(0, 200)
        );
        PopupDetalle.IsVisible = false;
        DimOverlay.IsVisible = false;
        _mascotaPopup = null;
    }

    private async void OnEditarDesdePupupTapped(object sender, EventArgs e)
    {
        if (_mascotaPopup is null) return;
        var mascota = _mascotaPopup;
        await CerrarPopupAsync();
        await Navigation.PushAsync(new MisMascotas(mascota));
    }

    private async void OnEliminarTapped(object sender, EventArgs e)
    {
        if (_mascotaPopup is null) return;
        bool ok = await DisplayAlert("Eliminar",
            $"¿Seguro que quieres eliminar a {_mascotaPopup.Name}?", "Eliminar", "Cancelar");
        if (!ok) return;

        // Borrar del backend si ya fue subida (para que desaparezca del feed público)
        if (_mascotaPopup.ApiId > 0)
        {
            try { await _api.EliminarMascotaAsync(_mascotaPopup.ApiId); } catch { }
        }

        await MascotaRepository.Instance.DeleteAsync(_mascotaPopup.Id);
        await CerrarPopupAsync();
        await CargarMascotasAsync();
        AplicarFiltro(_filtroActivo);
    }

    private async void OnAnadirMascotaTapped(object sender, EventArgs e)
        => await Navigation.PushAsync(new MisMascotas());

    private static string BuildEspecieLinea(MascotasItem m)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(m.Especie)) parts.Add(m.Especie);
        if (!string.IsNullOrEmpty(m.Race)) parts.Add(m.Race);
        return string.Join(" · ", parts);
    }

    private static string BuildEdadTexto(MascotasItem m)
    {
        if (m.FechaNacimiento.HasValue)
        {
            var diff = DateTime.Today - m.FechaNacimiento.Value;
            int years = (int)(diff.TotalDays / 365.25);
            int months = (int)((diff.TotalDays % 365.25) / 30.44);
            return years > 0
                ? $"{years} año{(years != 1 ? "s" : "")} y {months} mes{(months != 1 ? "es" : "")}"
                : $"{months} mes{(months != 1 ? "es" : "")}";
        }
        if (m.EdadAnios.HasValue || m.EdadMeses.HasValue)
        {
            var parts = new List<string>();
            if (m.EdadAnios is > 0) parts.Add($"{m.EdadAnios} año{(m.EdadAnios != 1 ? "s" : "")}");
            if (m.EdadMeses is > 0) parts.Add($"{m.EdadMeses} mes{(m.EdadMeses != 1 ? "es" : "")}");
            return parts.Count > 0 ? string.Join(" y ", parts) : "—";
        }
        return "—";
    }

    private void OnBuscarTapped(object? sender, EventArgs e) { }

    private async void OnFavoritosTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);
}