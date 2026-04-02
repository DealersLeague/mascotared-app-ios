using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Mascotared.Services;

namespace Mascotared;

public partial class MascotasPorDuenoPage : ContentPage
{
    private List<MascotaDestacada> _todasLasMascotas = new();
    private ObservableCollection<MascotasPorDuenoGrupo> _mascotasAgrupadas = new();

    public MascotasPorDuenoPage()
    {
        InitializeComponent();
    }

    // Constructor de compatibilidad (ya no usa la lista pasada — carga desde API)
    public MascotasPorDuenoPage(List<MascotaDestacada> mascotas) : this() { }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDesdeApiAsync();
    }

    private async Task CargarDesdeApiAsync()
    {
        var api = new ApiService();
        _todasLasMascotas = new List<MascotaDestacada>();

        try
        {
            // Obtener TODAS las mascotas públicas de la plataforma
            var todasMascotas = await api.GetMascotasPublicasAsync();

            foreach (var m in todasMascotas)
            {
                // Nombre de la mascota
                string nombre = m.TryGetProperty("nombre", out var n)
                    ? n.GetString() ?? "Mascota" : "Mascota";

                // Tipo de animal — el API devuelve "especie"
                string especie = m.TryGetProperty("especie", out var esp)
                    ? esp.GetString() ?? ""
                    : (m.TryGetProperty("tipoAnimal", out var ta) ? ta.GetString() ?? "" : "");

                // Nombre del dueño — el API devuelve "duenoNombre"
                string dueno = "";
                if (m.TryGetProperty("duenoNombre", out var dn) && dn.ValueKind == JsonValueKind.String)
                    dueno = dn.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(dueno))
                    dueno = "Propietario/a";

                // ID del dueño — el API devuelve "duenoId"
                string duenoId = "";
                if (m.TryGetProperty("duenoId", out var did))
                    duenoId = did.ValueKind == JsonValueKind.String
                        ? did.GetString() ?? ""
                        : did.ValueKind == JsonValueKind.Number
                            ? did.GetInt32().ToString() : "";

                // Foto de la mascota — el API devuelve "foto" (url o base64)
                string imagenUrl = "";
if (m.TryGetProperty("foto", out var foto) && foto.GetString() is string fotoStr && !string.IsNullOrEmpty(fotoStr))
{
    imagenUrl = fotoStr.StartsWith("http://")
        ? fotoStr.Replace("http://", "https://")
        : fotoStr.StartsWith("https://")
            ? fotoStr
            : fotoStr.StartsWith("data:")
                ? fotoStr
                : $"data:image/jpeg;base64,{fotoStr}";
}
                _todasLasMascotas.Add(new MascotaDestacada
                {
                    Nombre = nombre,
                    Dueno = dueno,
                    DuenoId = duenoId,
                    TipoAnimal = especie,
                    ImagenUrl = imagenUrl,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MascotasPorDueno] Error cargando desde API: {ex.Message}");
        }

        // Si no hay mascotas en la API pública, intentar cargarlas
        // de todos los usuarios con ofertas (propietario + cuidador)
        if (_todasLasMascotas.Count == 0)
        {
            await CargarDesdeTasksAsync(api);
        }

        CargarMascotas();
    }

    /// <summary>
    /// Fallback: carga mascotas buscando todos los usuarios que tienen offers
    /// (tanto propietarios como cuidadores) y pidiendo sus mascotas una a una.
    /// </summary>
    private async Task CargarDesdeTasksAsync(ApiService api)
    {
        try
        {
            // Obtener ofertas de propietarios Y cuidadores
            var tasksProp = await TasksRepository.Instance.GetAllAsync(esCuidador: false);
            var tasksCuid = await TasksRepository.Instance.GetAllAsync(esCuidador: true);

            var todosLosUsuarios = tasksProp.Concat(tasksCuid)
                .Where(t => !string.IsNullOrEmpty(t.UsuarioIdPublico))
                .GroupBy(t => t.UsuarioIdPublico)
                .Select(g => (UserId: g.Key!, Nombre: g.First().name))
                .ToList();

            var petTareas = todosLosUsuarios
                .Select(u => api.GetMascotasPublicasDeUsuarioAsync(u.UserId))
                .ToList();
            var resultados = await Task.WhenAll(petTareas);

            for (int i = 0; i < todosLosUsuarios.Count; i++)
            {
                var (userId, dueno) = todosLosUsuarios[i];
                foreach (var m in resultados[i])
                {
                    string nombre = m.TryGetProperty("nombre", out var n) ? n.GetString() ?? "Mascota" : "Mascota";
                    string especie = m.TryGetProperty("especie", out var esp)
                        ? esp.GetString() ?? ""
                        : (m.TryGetProperty("tipoAnimal", out var ta) ? ta.GetString() ?? "" : "");

                    string imagenUrl = string.Empty;
if (m.TryGetProperty("foto", out var foto) && foto.GetString() is string fotoStr && !string.IsNullOrEmpty(fotoStr))
{
    imagenUrl = fotoStr.StartsWith("http://")
        ? fotoStr.Replace("http://", "https://")
        : fotoStr.StartsWith("https://")
            ? fotoStr
            : fotoStr.StartsWith("data:")
                ? fotoStr
                : $"data:image/jpeg;base64,{fotoStr}";
}

                    _todasLasMascotas.Add(new MascotaDestacada
                    {
                        Nombre = nombre,
                        Dueno = dueno,
                        DuenoId = userId,
                        TipoAnimal = especie,
                        ImagenUrl = imagenUrl,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MascotasPorDueno] Fallback error: {ex.Message}");
        }
    }

    private void CargarMascotas()
    {
        var agrupadas = _todasLasMascotas
            .GroupBy(m => m.Dueno)
            .Select(g => new MascotasPorDuenoGrupo(g.Key, g.ToList()))
            .OrderBy(g => g.NombreDueno)
            .ToList();

        _mascotasAgrupadas = new ObservableCollection<MascotasPorDuenoGrupo>(agrupadas);
        ListaMascotas.ItemsSource = _mascotasAgrupadas;

        bool hayDatos = _todasLasMascotas.Count > 0;
        ListaMascotas.IsVisible = hayDatos;
        EstadoVacio.IsVisible = !hayDatos;
    }

    private void OnBusquedaChanged(object sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
        {
            CargarMascotas();
            return;
        }

        var filtradas = _todasLasMascotas
            .Where(m => m.Nombre.ToLower().Contains(texto) ||
                       m.Dueno.ToLower().Contains(texto) ||
                       m.TipoAnimal.ToLower().Contains(texto))
            .GroupBy(m => m.Dueno)
            .Select(g => new MascotasPorDuenoGrupo(g.Key, g.ToList()))
            .OrderBy(g => g.NombreDueno)
            .ToList();

        _mascotasAgrupadas.Clear();
        foreach (var grupo in filtradas)
            _mascotasAgrupadas.Add(grupo);
    }
}

public class MascotasPorDuenoGrupo : List<MascotaDestacada>
{
    public string NombreDueno { get; }

    public MascotasPorDuenoGrupo(string nombreDueno, List<MascotaDestacada> mascotas) : base(mascotas)
    {
        NombreDueno = nombreDueno;
    }
}
