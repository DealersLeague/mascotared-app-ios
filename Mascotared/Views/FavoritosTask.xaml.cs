using Mascotared.Models;
using Mascotared.Perfil;
using Mascotared.Services;
using Mascotared.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Mascotared.Views;

public partial class FavoritosTask : ContentPage
{
    private readonly ObservableCollection<FavoritoItem> _todas = new();
    private readonly ObservableCollection<FavoritoItem> _filtered = new();

    private string _activeFilter = "Todos";

    private readonly string _miId = Preferences.Get("user_id", string.Empty);
    private readonly ApiService _api = new();

    public FavoritosTask()
    {
        InitializeComponent();
        GridFavoritos.ItemsSource = _filtered;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CargarFavoritosAsync();
            ApplyFilter(_activeFilter);
        }
        catch (Exception ex) { Debug.WriteLine($"OnAppearing: {ex.Message}"); }
    }

    // ── Carga de datos ────────────────────────────────────────────────────

    private async Task CargarFavoritosAsync()
    {
        // Carga TODOS los perfiles favoritos
        var favoritos = await _api.GetFavoritosAsync();
        _todas.Clear();
        foreach (var f in favoritos) _todas.Add(f);
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
                { "Propietarios",           propRequests      },
                { "Cuidadores",             helperRequests    },
            };

            foreach (var (key, chip) in chips)
            {
                bool active = key == filter;
                chip.BackgroundColor = active ? Color.FromArgb("#455AEB") : Colors.White;
                chip.Stroke = active ? Colors.Transparent : Color.FromArgb("#EEF0FB");
                if (chip.Content is Label lbl)
                    lbl.TextColor = active ? Colors.White : Color.FromArgb("#9AA0C4");
            }

            _filtered.Clear();

            IEnumerable<FavoritoItem> resultado = filter switch
            {
                "Propietarios" => _todas.Where(f =>
                    f.Tags.Contains("Propietario")),

                "Cuidadores" => _todas.Where(f =>
                    f.Tags.Contains("Cuidador")),

                // Todos: solo las mías (creadas o aceptadas), no las de otros
                _ => _todas,
            };

            foreach (var m in resultado) _filtered.Add(m);
            EstadoVacio.IsVisible = _filtered.Count == 0;
            GridFavoritos.IsVisible = _filtered.Count > 0;
        }
        catch (Exception ex) { Debug.WriteLine($"ApplyFilter: {ex.Message}"); }
    }

    // ── Tap tarjeta → popup ───────────────────────────────────────────────

    private async void OnFavoritoSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not FavoritoItem favorito) return;

        if (_miId == favorito.CuidadorId)
            await Navigation.PushAsync(new PerfilPublico());
        else
            await Navigation.PushAsync(new PerfilPublico(favorito.CuidadorId, favorito.Foto, favorito.Nombre));
    }

    private async void OnHeartTapped(object sender, EventArgs e)
    {
        if (sender is not BindableObject bo || bo.BindingContext is not FavoritoItem favorito) return;
        GridFavoritos.SelectedItem = null;

        if (await _api.EliminarFavoritoAsync(favorito.CuidadorId))
        {
            await CargarFavoritosAsync();
            ApplyFilter(_activeFilter);
        }
    }

    private async void OnAnadirTaskTapped(object sender, EventArgs e)
        => await Navigation.PushAsync(new Tasks());

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