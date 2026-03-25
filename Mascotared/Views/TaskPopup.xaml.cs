using Mascotared.Models;
using Mascotared.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Mascotared.Views;

public partial class TaskPopup : ContentPage
{
    private readonly ObservableCollection<TaskItem> _todas = new();
    private readonly ObservableCollection<TaskItem> _filtered = new();
    private readonly ObservableCollection<SolicitudItem> _solicitudes = new();

    private string _activeFilter = "Todos";
    private TaskItem? _taskPopup;

    private readonly string _miId = Preferences.Get("user_id", string.Empty);
    private readonly ApiService _api = new();

    public TaskPopup()
    {
        InitializeComponent();
        GridMascotas.ItemsSource = _filtered;
        ListaSolicitudes.ItemsSource = _solicitudes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CargarTasksAsync();
            await CargarSolicitudesEnviadasAsync();
            ApplyFilter(_activeFilter);
        }
        catch (Exception ex) { Debug.WriteLine($"OnAppearing: {ex.Message}"); }

        try
        {
            var convs = await _api.GetConversacionesAsync();
            BadgeNoLeidos.IsVisible = convs.Any(j =>
                j.TryGetProperty("noLeidos", out var nl) && nl.GetInt32() > 0);
        }
        catch { }
    }

    // ── Carga de datos ────────────────────────────────────────────────────

    private async Task CargarTasksAsync()
    {
        var lista = await TasksRepository.Instance.GetAllAsync();
        _todas.Clear();
        foreach (var t in lista) _todas.Add(t);
    }

    private async Task CargarSolicitudesEnviadasAsync()
    {
        try
        {
            var lista = await _api.GetMisSolicitudesAsync();
            _solicitudes.Clear();
            foreach (var s in lista) _solicitudes.Add(s);
        }
        catch (Exception ex) { Debug.WriteLine($"CargarSolicitudes: {ex.Message}"); }
    }

    // ── Filtros ───────────────────────────────────────────────────────────

    private void OnFilterTapped(object sender, EventArgs e)
    {
        if (sender is not Border chip) return;
        var tap = (TapGestureRecognizer)chip.GestureRecognizers[0];
        _activeFilter = tap.CommandParameter?.ToString() ?? "Todos";
        ApplyFilter(_activeFilter);
    }

    private void ApplyFilter(string filter)
    {
        try
        {
            var chips = new Dictionary<string, Border>
            {
                { "Todos",                  allRequests       },
                { "Solicitudes de cuidado", propRequests      },
                { "Tu Cuidado",             helperRequests    },
                { "Completadas",            completedRequests },
                { "Otras Solicitudes",      otrasRequests     },
            };

            foreach (var (key, chip) in chips)
            {
                bool active = key == filter;
                chip.BackgroundColor = active ? Color.FromArgb("#455AEB") : Colors.White;
                chip.Stroke = active ? Colors.Transparent : Color.FromArgb("#EEF0FB");
                if (chip.Content is Label lbl)
                    lbl.TextColor = active ? Colors.White : Color.FromArgb("#9AA0C4");
            }

            bool esOtrasSolicitudes = filter == "Otras Solicitudes";
            ListaSolicitudes.IsVisible = esOtrasSolicitudes;
            GridMascotas.IsVisible = !esOtrasSolicitudes;

            if (esOtrasSolicitudes)
            {
                EstadoVacio.IsVisible = _solicitudes.Count == 0;
                return;
            }

            _filtered.Clear();

            IEnumerable<TaskItem> resultado = filter switch
            {
                "Solicitudes de cuidado" => _todas.Where(t => t.EsMia && !t.caretaker && !t.IsFinished),
                "Tu Cuidado" => _todas.Where(t =>
                    (t.EsAceptado && !t.IsFinished) ||
                    (t.EsMia && t.caretaker && !t.IsFinished)),
                "Completadas" => _todas.Where(t => t.IsFinished && (t.EsMia || t.EsAceptado)),
                _ => _todas.Where(t => t.EsMia || t.EsAceptado),
            };

            foreach (var m in resultado) _filtered.Add(m);
            EstadoVacio.IsVisible = _filtered.Count == 0;
            GridMascotas.IsVisible = _filtered.Count > 0;
        }
        catch (Exception ex) { Debug.WriteLine($"ApplyFilter: {ex.Message}"); }
    }

    // ── Tap tarjeta → popup ───────────────────────────────────────────────

    private void OnTaskSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TaskItem task) return;
        GridMascotas.SelectedItem = null;
        _ = MostrarPopupAsync(task);
    }

    private async Task MostrarPopupAsync(TaskItem t)
    {
        _taskPopup = t;

        bool soyAutor = t.EsMia;
        bool soyCuidador = t.EsAceptado;

        PopupProp.IsVisible = !t.caretaker;
        PopupCare.IsVisible = t.caretaker;

        PopupFoto.Source = string.IsNullOrEmpty(t.photoPath)
            ? "placeholder_pet.png"
            : t.photoPath.StartsWith("http")
                ? ImageSource.FromUri(new Uri(t.photoPath))
                : ImageSource.FromFile(t.photoPath);

        PopupTitle.Text = t.title;
        PopupLocation.Text = await BuildLocationAsync(t);

        PopupPills.Children.Clear();
        AddPill(string.Join(' ', t.tags), "#F0F1FD", "#455AEB");
        AddPill(t.description, "#F0F1FD", "#455AEB");
        AddPill($"{(!t.caretaker ? t.totalPrice : t.perHour)} €", "#F5FFF0", "#22AA55");

        if (!t.caretaker)
        {
            PopupCareDate.Text = BuildCareDate(t);
            PopupExactTime.Text = BuildExactTime(t);
            PopupSchedule.Text = BuildSchedule(t);
            BuildPetList(t);
            PopupSpecialNeeds.Text = t.specialNeeds ? "Sí" : "No";
            PopupSpecialNeedsDesc.Text = t.specialNeedsDes ?? "-";
        }
        else
        {
            PopupWeekCare.Text = t.weekDays?.Count > 0 ? string.Join(" - ", t.weekDays) : "—";
            PopupScheduleCare.Text = BuildSchedule(t);
            PopupHadPets.Text = t.hadPets ? "Sí" : "No";
            PopupMaxPets.Text = t.maxPets > 0 ? t.maxPets.ToString() : "—";
            PopupPetCare.Text = t.canLookafter?.Count > 0 ? string.Join(" - ", t.canLookafter) : "—";
            PopupSpecialNeedsCare.Text = t.specialNeeds ? "Sí" : "No";
            PopupSpecialNeedsXP.Text = t.specialNeedsDes ?? "-";
        }

        // Botones según rol
        BotonesAccion.IsVisible = soyAutor && !t.IsFinished;
        BtnFinalizar.IsVisible = soyAutor && !t.IsFinished;
        BtnVerSolicitudes.IsVisible = soyAutor && !t.IsFinished;
        BtnCompletar.IsVisible = soyCuidador && !soyAutor && !t.IsFinished;
        // El cuidador puede reseñar al propietario, y el propietario puede reseñar al cuidador
        BtnDejarResena.IsVisible = t.IsFinished &&
            ((soyCuidador && !soyAutor) ||
             (soyAutor && !string.IsNullOrEmpty(t.AceptadoPorId)));
        LabelCompletada.IsVisible = t.IsFinished;

        // Cargar solicitudes recibidas si soy el autor
        StackSolicitudesRecibidas.Children.Clear();
        SeccionSolicitudesRecibidas.IsVisible = false;
        if (soyAutor && !t.IsFinished)
            await CargarSolicitudesRecibidasEnTareaAsync(t.offerId);

        // ── FIX: Cargar reseñas del autor de la tarea ─────────────────────
        await CargarResenasAutorAsync(t.UsuarioIdPublico);

        // Animación
        PopupDetalle.TranslationY = 600;
        PopupDetalle.IsVisible = true;
        DimOverlay.IsVisible = true;
        _ = PopupDetalle.TranslateToAsync(0, 0, 280, Easing.CubicOut);
        _ = DimOverlay.FadeTo(1, 220);
    }

    // ── Reseñas del autor de la tarea ─────────────────────────────────────

    private async Task CargarResenasAutorAsync(string? autorId)
    {
        // Ocultamos la sección por defecto
        SeccionResenas.IsVisible = false;
        StackResenas.Children.Clear();

        if (string.IsNullOrEmpty(autorId)) return;

        try
        {
            var resultado = await _api.GetReviewsDeUsuarioAsync(autorId);
            if (resultado == null || resultado.Reviews.Count == 0) return;

            // Título con media y total
            LabelResenasHeader.Text =
                $"⭐ {resultado.Media:F1}  ·  {resultado.Total} reseña{(resultado.Total != 1 ? "s" : "")}";

            foreach (var r in resultado.Reviews)
                StackResenas.Children.Add(CrearFilaResena(r));

            SeccionResenas.IsVisible = true;
        }
        catch (Exception ex) { Debug.WriteLine($"CargarResenas: {ex.Message}"); }
    }

    private static Border CrearFilaResena(ReviewItem r)
    {
        string estrellas = new string('⭐', r.Puntuacion);

        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Children.Add(new Label
        {
            Text = $"{estrellas}  {r.AutorNombre}",
            FontFamily = "InterSemiBold",
            FontSize = 13,
            TextColor = Color.FromArgb("#2C2C2C")
        });

        if (!string.IsNullOrEmpty(r.Comentario))
            stack.Children.Add(new Label
            {
                Text = r.Comentario,
                FontSize = 12,
                TextColor = Color.FromArgb("#666680"),
                LineBreakMode = LineBreakMode.WordWrap
            });

        stack.Children.Add(new Label
        {
            Text = r.FechaCreacion.ToString("dd/MM/yyyy"),
            FontSize = 11,
            TextColor = Color.FromArgb("#9AA0C4")
        });

        return new Border
        {
            BackgroundColor = Color.FromArgb("#F8F9FF"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            Stroke = Color.FromArgb("#EEF0FB"),
            StrokeThickness = 1,
            Padding = new Thickness(14, 10),
            Margin = new Thickness(0, 4),
            Content = stack
        };
    }

    // ── Solicitudes recibidas en la tarea del popup ───────────────────────

    private async Task CargarSolicitudesRecibidasEnTareaAsync(int ofertaId)
    {
        try
        {
            var lista = await _api.GetSolicitudesRecibidasAsync(ofertaId);
            if (lista.Count == 0) return;

            SeccionSolicitudesRecibidas.IsVisible = true;

            foreach (var s in lista)
            {
                var fila = CrearFilaSolicitudRecibida(s);
                StackSolicitudesRecibidas.Children.Add(fila);
            }
        }
        catch (Exception ex) { Debug.WriteLine($"SolicitudesRecibidas: {ex.Message}"); }
    }

    private View CrearFilaSolicitudRecibida(SolicitudRecibidaItem s)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
            },
            Padding = new Thickness(0, 6)
        };

        var info = new VerticalStackLayout { Spacing = 2 };
        info.Children.Add(new Label
        {
            Text = s.SolicitanteNombre,
            FontFamily = "InterSemiBold",
            FontSize = 14,
            TextColor = Color.FromArgb("#2C2C2C")
        });
        info.Children.Add(new Label
        {
            Text = s.Mensaje ?? "Sin mensaje",
            FontSize = 12,
            TextColor = Color.FromArgb("#9AA0C4"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        var btnAceptar = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            BackgroundColor = Color.FromArgb("#455AEB"),
            Stroke = Colors.Transparent,
            Padding = new Thickness(14, 8),
            IsVisible = s.Estado == "Pendiente",
            Content = new Label
            {
                Text = "Aceptar",
                TextColor = Colors.White,
                FontFamily = "InterSemiBold",
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var solicitanteId = s.SolicitanteId;
        var ofertaId = s.OfertaId;

        btnAceptar.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                bool ok = await DisplayAlert("Aceptar solicitud",
                    $"¿Aceptar a {s.SolicitanteNombre}? La tarea dejará de ser visible para otros.",
                    "Aceptar", "Cancelar");
                if (!ok) return;

                bool exito = await _api.AceptarSolicitanteAsync(ofertaId, solicitanteId);
                if (exito)
                {
                    await CerrarPopupAsync();
                    await CargarTasksAsync();
                    ApplyFilter(_activeFilter);
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo aceptar la solicitud", "OK");
                }
            })
        });

        var lblEstado = new Label
        {
            Text = s.Estado,
            FontSize = 12,
            FontFamily = "InterSemiBold",
            TextColor = s.Estado == "Aceptada" ? Color.FromArgb("#22AA55") : Color.FromArgb("#9AA0C4"),
            IsVisible = s.Estado != "Pendiente",
            VerticalOptions = LayoutOptions.Center
        };

        grid.Add(info, 0);
        grid.Add(s.Estado == "Pendiente" ? (View)btnAceptar : lblEstado, 1);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#F4F6FA"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            Stroke = Colors.Transparent,
            Padding = new Thickness(14, 10),
            Margin = new Thickness(0, 4),
            Content = grid
        };
    }

    // ── Botón Finalizar (autor) ───────────────────────────────────────────

    private async void OnFinalizarTapped(object sender, EventArgs e)
    {
        if (_taskPopup is null) return;

        bool ok = await DisplayAlertAsync("Finalizar tarea",
            $"¿Marcar «{_taskPopup.title}» como completada? Esta acción no se puede deshacer.",
            "Finalizar", "Cancelar");
        if (!ok) return;

        bool exito = await _api.FinalizarOfertaAsync(_taskPopup.offerId);
        if (exito)
        {
            if (!string.IsNullOrEmpty(_taskPopup.AceptadoPorId))
                await PedirResenaAsync(_taskPopup.AceptadoPorId, _taskPopup.title);

            await CerrarPopupAsync();
            await CargarTasksAsync();
            ApplyFilter("Completadas");
        }
        else
        {
            await DisplayAlert("Error", "No se pudo finalizar la tarea", "OK");
        }
    }

    // ── Botón Completar (cuidador aceptado) ───────────────────────────────

    private async void OnCompletarTapped(object sender, EventArgs e)
    {
        if (_taskPopup is null) return;

        bool ok = await DisplayAlert("Marcar como completada",
            $"¿Confirmas que has completado «{_taskPopup.title}»?",
            "Sí, completada", "Cancelar");
        if (!ok) return;

        bool exito = await _api.FinalizarOfertaAsync(_taskPopup.offerId);
        if (exito)
        {
            if (!string.IsNullOrEmpty(_taskPopup.UsuarioIdPublico))
                await PedirResenaAsync(_taskPopup.UsuarioIdPublico, _taskPopup.title);

            await CerrarPopupAsync();
            await CargarTasksAsync();
            ApplyFilter("Completadas");
        }
        else
        {
            await DisplayAlert("Error", "No se pudo completar la tarea", "OK");
        }
    }

    // ── Botón Dejar Reseña (cuidador, tarea ya finalizada) ───────────────

    private async void OnDejarResenaTapped(object sender, EventArgs e)
    {
        if (_taskPopup is null) return;

        // El propietario (autor) reseña al cuidador aceptado; el cuidador reseña al propietario
        string? objetivoId = _taskPopup.EsMia
            ? _taskPopup.AceptadoPorId        // soy propietario → reseño al cuidador
            : _taskPopup.UsuarioIdPublico;    // soy cuidador    → reseño al propietario

        if (!string.IsNullOrEmpty(objetivoId))
            await PedirResenaAsync(objetivoId, _taskPopup.title);
        else
            await DisplayAlert("Error", "No se puede identificar a la otra persona.", "OK");
    }

    // ── Flujo de reseña ───────────────────────────────────────────────────

    private async Task PedirResenaAsync(string usuarioId, string tituloTarea)
    {
        bool quiereResena = await DisplayAlert(
            "¿Dejar una reseña?",
            $"¿Quieres valorar a la otra persona por «{tituloTarea}»?",
            "Sí, valorar", "Ahora no");

        if (!quiereResena) return;

        string? puntuacionStr = await DisplayActionSheet(
            "¿Cuántas estrellas?", "Cancelar", null,
            "⭐ 1", "⭐⭐ 2", "⭐⭐⭐ 3", "⭐⭐⭐⭐ 4", "⭐⭐⭐⭐⭐ 5");

        if (puntuacionStr == null || puntuacionStr == "Cancelar") return;

        int puntuacion = puntuacionStr.Count(c => c == '⭐');

        string comentario = await DisplayPromptAsync(
            "Comentario (opcional)",
            "Escribe algo sobre tu experiencia",
            placeholder: "Todo fue genial...",
            maxLength: 200);

        bool okResena = await _api.CrearReviewAsync(usuarioId, puntuacion, comentario ?? "", _taskPopup?.offerId); if (okResena)
            await DisplayAlert("¡Gracias!", "Tu reseña ha sido enviada.", "OK");
    }

    // ── Ver solicitudes recibidas (botón extra) ───────────────────────────

    private void OnVerSolicitudesTapped(object sender, EventArgs e)
    {
        SeccionSolicitudesRecibidas.IsVisible = !SeccionSolicitudesRecibidas.IsVisible;
    }

    // ── Popup helpers ─────────────────────────────────────────────────────

    private void AddPill(string? text, string bg, string fg)
    {
        if (string.IsNullOrEmpty(text)) return;
        PopupPills.Children.Add(new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            BackgroundColor = Color.FromArgb(bg),
            Stroke = Colors.Transparent,
            Padding = new Thickness(10, 4),
            Margin = new Thickness(0, 0, 6, 6),
            Content = new Label { Text = text, FontSize = 12, TextColor = Color.FromArgb(fg), FontFamily = "InterSemiBold" }
        });
    }

    private async void OnCerrarPopupTapped(object sender, EventArgs e) => await CerrarPopupAsync();

    private async Task CerrarPopupAsync()
    {
        await Task.WhenAll(
            PopupDetalle.TranslateTo(0, 600, 240, Easing.CubicIn),
            DimOverlay.FadeTo(0, 200)
        );
        PopupDetalle.IsVisible = false;
        DimOverlay.IsVisible = false;
        _taskPopup = null;
        SeccionSolicitudesRecibidas.IsVisible = false;
        SeccionResenas.IsVisible = false;
    }

    private async void OnEditarDesdePupupTapped(object sender, EventArgs e)
    {
        if (_taskPopup is null) return;
        var task = _taskPopup;
        await CerrarPopupAsync();
        await Navigation.PushAsync(new Tasks(task));
    }

    private async void OnEliminarTapped(object sender, EventArgs e)
    {
        if (_taskPopup is null) return;
        bool ok = await DisplayAlert("Eliminar",
            $"¿Seguro que quieres eliminar «{_taskPopup.title}»?", "Eliminar", "Cancelar");
        if (!ok) return;

        await TasksRepository.Instance.DeleteAsync(_taskPopup.offerId);
        await CerrarPopupAsync();
        await CargarTasksAsync();
        ApplyFilter(_activeFilter);
    }

    private async void OnAnadirTaskTapped(object sender, EventArgs e)
        => await Navigation.PushAsync(new Tasks());

    // ── Cancelar solicitud enviada (tab Otras Solicitudes) ────────────────

    private async void OnCancelarSolicitudTapped(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not int solicitudId) return;
        bool ok = await DisplayAlert("Cancelar", "¿Retirar esta solicitud?", "Sí", "No");
        if (!ok) return;

        if (await _api.CancelarSolicitudAsync(solicitudId))
            await CargarSolicitudesEnviadasAsync();
        else
            await DisplayAlert("Error", "No se pudo cancelar", "OK");
    }

    // ── Build helpers ─────────────────────────────────────────────────────

    private static async Task<string> BuildLocationAsync(TaskItem t)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(t.userLocation)) parts.Add(t.userLocation);
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var loc = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(5)));
                if (loc != null)
                {
                    var d = t.DistanceCalculator(loc);
                    if (!double.IsNaN(d) && d > 0) parts.Add($"{d:F1} Km");
                }
            }
        }
        catch { }
        return string.Join(" · ", parts);
    }

    private static string BuildExactTime(TaskItem t)
    {
        if (!t.exactHours) return "—";
        return $"{t.timeSpan0?.ToString(@"hh\:mm") ?? "—"} - {t.timeSpan1?.ToString(@"hh\:mm") ?? "—"}";
    }

    private static string BuildCareDate(TaskItem t)
    {
        var d0 = t.date0?.ToString("dd/MM/yyyy") ?? "—";
        var d1 = t.date1?.ToString("dd/MM/yyyy") ?? "—";
        return t.date0 == t.date1 ? d0 : $"{d0} - {d1}";
    }

    private static string BuildSchedule(TaskItem t)
        => t.timeOfDay?.Count > 0 ? string.Join(" - ", t.timeOfDay) : "—";

    // ── FIX: BuildPetList filtra por el usuario activo ────────────────────
    private async void BuildPetList(TaskItem t)
    {
        StackMascotas.Children.Clear();
        var miId = Preferences.Get("user_id", string.Empty);
        var mascotas = await MascotaRepository.Instance.GetAllAsync();
        var misMascotas = mascotas.Where(m => m.UsuarioId == miId).ToList();

        foreach (var v in t.petList.Where(x => !string.IsNullOrEmpty(x)))
        {
            var m = misMascotas.FirstOrDefault(x => x.Name == v);
            if (m != null) StackMascotas.Children.Add(CrearFilaMascota(m));
        }
    }

    private static Border CrearFilaMascota(MascotasItem m)
    {
        string emoji = m.Especie?.ToLower() switch
        {
            "perro" => "🐕",
            "gato" => "🐈",
            "conejo" => "🐇",
            "hámster" => "🐹",
            "reptil" => "🦎",
            "ave" => "🦜",
            "caballo" => "🐎",
            _ => "🐾"
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        grid.Add(new Label { Text = emoji, FontSize = 32, VerticalOptions = LayoutOptions.Start }, 0);

        var info = new VerticalStackLayout { Spacing = 4 };
        info.Children.Add(new Label { Text = m.Name, FontFamily = "OpenSansSemibold", FontSize = 14, TextColor = Color.FromArgb("#2C2C2C") });
        var sub = new List<string>();
        if (!string.IsNullOrEmpty(m.Race)) sub.Add(m.Race);
        if (!string.IsNullOrEmpty(m.Especie)) sub.Add(m.Especie);
        if (sub.Count > 0)
            info.Children.Add(new Label { Text = string.Join(" · ", sub), FontSize = 12, TextColor = Color.FromArgb("#9AA0C4") });
        grid.Add(info, 1);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#F4F6FA"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
            Stroke = Colors.Transparent,
            Padding = new Thickness(14),
            Content = grid
        };
    }

    // ── Bottom nav ────────────────────────────────────────────────────────

    private void OnBuscarTapped(object? sender, EventArgs e) { }

    private async void OnFavoritosTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private async void OnMensajesTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToMessages(Navigation);

    private async void OnCuentaTapped(object? sender, EventArgs e)
        => await NavigationHelper.NavigateToPerfil(Navigation);

    // ── Stubs para compatibilidad con XAML (formulario embebido) ─────────
    private void OnSeleccionarFotoTapped(object sender, EventArgs e) { }
    private void OnFormChanged(object sender, EventArgs e) { }
    private void OnCreateTapped(object sender, EventArgs e) { }
}