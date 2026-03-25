using System.Collections.ObjectModel;
using Mascotared.Models;
using Mascotared.Services;

namespace Mascotared;

public partial class Messages : ContentPage
{
    private readonly ObservableCollection<ConversacionItem> _todas = new();
    private readonly ObservableCollection<ConversacionItem> _filtradas = new();
    private readonly ApiService _api = new();
    private readonly string _miId = Preferences.Get("user_id", string.Empty);

    public Messages()
    {
        InitializeComponent();
        ListaConversaciones.ItemsSource = _filtradas;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Mostrar local inmediatamente (funciona offline)
        await CargarLocalesAsync();

        // 2. Sincronizar con API en background (actualiza si hay conexión)
        _ = SincronizarConApiAsync();
    }

    // ── 1. Carga rápida desde local ───────────────────────────────────────
    private async Task CargarLocalesAsync()
    {
        var locales = await ConversacionesRepository.Instance.GetAllAsync();
        _todas.Clear();
        foreach (var c in locales.Where(c => !c.Archivada).OrderByDescending(c => c.FechaUltimoMensaje))
            _todas.Add(c);

        RefrescarLista();
        BadgeNoLeidos.IsVisible = _todas.Any(c => c.NoLeidos > 0);
        await ActualizarSeccionArchivadosAsync(locales);
    }

    // ── Sección archivados ────────────────────────────────────────────────
    private async Task ActualizarSeccionArchivadosAsync(List<ConversacionItem>? todas = null)
    {
        todas ??= await ConversacionesRepository.Instance.GetAllAsync();
        var archivadas = todas.Where(c => c.Archivada).OrderByDescending(c => c.FechaUltimoMensaje).ToList();

        SeccionArchivados.IsVisible = archivadas.Count > 0;
        if (!SeccionArchivados.IsVisible) return;

        LblToggleArchivados.Text = $"📦  {archivadas.Count} archivado{(archivadas.Count == 1 ? "" : "s")}";

        // Repoblar la lista si está expandida
        if (StackArchivados.IsVisible)
            PoblarStackArchivados(archivadas);
    }

    private void PoblarStackArchivados(List<ConversacionItem> archivadas)
    {
        StackArchivados.Children.Clear();
        foreach (var conv in archivadas)
            StackArchivados.Children.Add(CrearItemArchivado(conv));
    }

    private View CrearItemArchivado(ConversacionItem conv)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 14,
            Padding = new Thickness(16, 12)
        };

        // Avatar
        var avatarBorder = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(26) },
            BackgroundColor = Color.FromArgb(conv.ColorAvatar),
            Stroke = Colors.Transparent,
            WidthRequest = 52, HeightRequest = 52,
            Content = new Label
            {
                Text = conv.Inicial,
                FontFamily = "InterSemiBold", FontSize = 20,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            },
            Opacity = 0.6
        };

        var textStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 3 };
        textStack.Children.Add(new Label { Text = conv.NombreContacto, FontFamily = "InterSemiBold", FontSize = 15, TextColor = Color.FromArgb("#9AA0C4"), LineBreakMode = LineBreakMode.TailTruncation });
        textStack.Children.Add(new Label { Text = conv.UltimoMensaje, FontSize = 13, TextColor = Color.FromArgb("#9AA0C4"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });

        grid.Add(avatarBorder, 0);
        grid.Add(textStack, 1);

        // Tap → acción
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) =>
        {
            string accion = await DisplayActionSheet(
                conv.NombreContacto, "Cancelar", null,
                "Ir al chat", "Desarchivar", "Eliminar");

            if (accion == "Ir al chat")
            {
                await Navigation.PushAsync(new Views.ChatPage(conv));
            }
            else if (accion == "Desarchivar")
            {
                await ConversacionesRepository.Instance.DesarchivarAsync(conv.ContactoId);
                _todas.Add(conv);
                _todas.Move(_todas.Count - 1, 0); // poner arriba del todo
                RefrescarLista();
                BadgeNoLeidos.IsVisible = _todas.Any(c => c.NoLeidos > 0);
                await ActualizarSeccionArchivadosAsync();
            }
            else if (accion == "Eliminar")
            {
                bool ok = await DisplayAlert("Eliminar",
                    $"¿Borrar el chat con {conv.NombreContacto}?", "Eliminar", "Cancelar");
                if (!ok) return;
                await ConversacionesRepository.Instance.EliminarAsync(conv.ContactoId);
                await ActualizarSeccionArchivadosAsync();
            }
        };
        grid.GestureRecognizers.Add(tap);

        return new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                grid,
                new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#EEF0FB"), Margin = new Thickness(82, 0, 0, 0) }
            }
        };
    }

    private void OnToggleArchivados(object sender, EventArgs e)
    {
        bool abrir = !StackArchivados.IsVisible;
        StackArchivados.IsVisible = abrir;
        LblChevronArchivados.Text = abrir ? "⌄" : "›";

        if (abrir)
        {
            _ = Task.Run(async () =>
            {
                var todas = await ConversacionesRepository.Instance.GetAllAsync();
                var arch = todas.Where(c => c.Archivada).OrderByDescending(c => c.FechaUltimoMensaje).ToList();
                await MainThread.InvokeOnMainThreadAsync(() => PoblarStackArchivados(arch));
            });
        }
    }

    // ── 2. Sincronización con API en background ───────────────────────────
    private async Task SincronizarConApiAsync()
    {
        try
        {
            var lista = await _api.GetConversacionesAsync();
            var remotas = new List<ConversacionItem>();

            foreach (var j in lista)
            {
                try
                {
                    var contactoId = j.GetProperty("usuarioId").GetString() ?? "";
                    var nombre = j.GetProperty("nombre").GetString() ?? "";
                    var ultimo = j.TryGetProperty("ultimoMensaje", out var um) ? um.GetString() ?? "" : "";
                    var fecha = j.TryGetProperty("fecha", out var f) ? f.GetDateTime() : DateTime.Now;
                    var noLeidos = j.TryGetProperty("noLeidos", out var nl) ? nl.GetInt32() : 0;
                    string? foto = j.TryGetProperty("fotoPerfil", out var fp) ? fp.GetString() : null;

                    remotas.Add(new ConversacionItem
                    {
                        ContactoId = contactoId,
                        NombreContacto = nombre,
                        UltimoMensaje = ultimo,
                        FechaUltimoMensaje = fecha,
                        NoLeidos = noLeidos,
                        FotoContacto = foto,
                        TituloSolicitud = "Solicitud de cuidado",
                        Valoracion = "Sin valoraciones",
                        TiempoRespuesta = "Activo: Recientemente"
                    });
                }
                catch { }
            }

            // Fusionar en local y persistir
            await ConversacionesRepository.Instance.SincronizarDesdeApiAsync(remotas);

            // Recargar UI con datos frescos
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await CargarLocalesAsync());
        }
        catch { }  // Sin conexión — la UI ya muestra los locales
    }

    private void RefrescarLista(string filtro = "")
    {
        _filtradas.Clear();

        var resultado = string.IsNullOrWhiteSpace(filtro)
            ? (IEnumerable<ConversacionItem>)_todas
            : _todas.Where(c =>
                c.NombreContacto.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                c.UltimoMensaje.Contains(filtro, StringComparison.OrdinalIgnoreCase));

        foreach (var c in resultado) _filtradas.Add(c);

        bool hay = _filtradas.Count > 0;
        EstadoVacio.IsVisible = !hay;
        ListaConversaciones.IsVisible = hay;
    }

    private void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
        => RefrescarLista(e.NewTextValue ?? "");

    private async void OnConversacionSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ConversacionItem conv) return;
        ListaConversaciones.SelectedItem = null;

        // Marcar como leída localmente al abrir
        await ConversacionesRepository.Instance.MarcarLeidaAsync(conv.ContactoId);
        conv.NoLeidos = 0;
        BadgeNoLeidos.IsVisible = _todas.Any(c => c.NoLeidos > 0);

        await Navigation.PushAsync(new Views.ChatPage(conv));
    }

    // ── Tap en avatar → perfil público del contacto ───────────────────────
    private async void OnVerPerfilContacto(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ConversacionItem conv) return;
        await Navigation.PushAsync(
            new Mascotared.Perfil.PerfilPublico(conv.ContactoId, conv.FotoContacto, conv.NombreContacto));
    }

    // ── Swipe: archivar chat ──────────────────────────────────────────────
    private async void OnArchivarChat(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.BindingContext is ConversacionItem conv)
        {
            await ConversacionesRepository.Instance.ArchivarAsync(conv.ContactoId);
            _todas.Remove(conv);
            RefrescarLista();
            BadgeNoLeidos.IsVisible = _todas.Any(c => c.NoLeidos > 0);
        }
    }

    // ── Swipe: eliminar chat ──────────────────────────────────────────────
    private async void OnEliminarChat(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.BindingContext is ConversacionItem conv)
        {
            bool ok = await DisplayAlert("Eliminar conversación",
                $"¿Borrar el chat con {conv.NombreContacto}? Esta acción no se puede deshacer.",
                "Eliminar", "Cancelar");
            if (!ok) return;
            await ConversacionesRepository.Instance.EliminarAsync(conv.ContactoId);
            _todas.Remove(conv);
            RefrescarLista();
            BadgeNoLeidos.IsVisible = _todas.Any(c => c.NoLeidos > 0);
        }
    }

    private async void OnExplorarTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnBuscarTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToMainPage(Navigation);

    private async void OnFavoritosTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToFavoritos(Navigation);

    private async void OnReservasTapped(object sender, EventArgs e)
        => await NavigationHelper.NavigateToSolicitudes(Navigation);

    private void OnMensajesTapped(object sender, EventArgs e) { }

    private async void OnCuentaTapped(object sender, EventArgs e)
        => await Navigation.PushAsync(new PerfilConfigUser());
}