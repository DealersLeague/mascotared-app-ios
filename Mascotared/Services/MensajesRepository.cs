using System.Text.Json;

namespace Mascotared.Services
{
    public class MensajesRepository
    {
        private static MensajesRepository? _instance;
        public static MensajesRepository Instance => _instance ??= new MensajesRepository();
        private MensajesRepository() { }

        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private string FilePath(int conversacionId) => Path.Combine(
            FileSystem.AppDataDirectory, $"chat_{conversacionId}.json");

        public async Task<List<MensajeGuardado>> GetMensajesAsync(int conversacionId)
        {
            await _lock.WaitAsync();
            try
            {
                string path = FilePath(conversacionId);
                if (!File.Exists(path)) return new List<MensajeGuardado>();
                string json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<List<MensajeGuardado>>(json, _opts)
                       ?? new List<MensajeGuardado>();
            }
            finally { _lock.Release(); }
        }

        public async Task GuardarMensajeAsync(int conversacionId, MensajeGuardado mensaje)
        {
            await _lock.WaitAsync();
            try
            {
                string path = FilePath(conversacionId);
                List<MensajeGuardado> lista;

                if (File.Exists(path))
                {
                    string json = await File.ReadAllTextAsync(path);
                    lista = JsonSerializer.Deserialize<List<MensajeGuardado>>(json, _opts)
                            ?? new List<MensajeGuardado>();
                }
                else
                {
                    lista = new List<MensajeGuardado>();
                }

                mensaje.Id = lista.Count > 0 ? lista.Max(m => m.Id) + 1 : 1;
                lista.Add(mensaje);

                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(lista, _opts));
            }
            finally { _lock.Release(); }
        }

        public async Task MarcarComoLeidoAsync(int conversacionId, int mensajeId)
        {
            await _lock.WaitAsync();
            try
            {
                string path = FilePath(conversacionId);
                if (!File.Exists(path)) return;

                string json = await File.ReadAllTextAsync(path);
                var lista = JsonSerializer.Deserialize<List<MensajeGuardado>>(json, _opts)
                            ?? new List<MensajeGuardado>();

                var m = lista.FirstOrDefault(x => x.Id == mensajeId);
                if (m != null) m.Leido = true;

                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(lista, _opts));
            }
            finally { _lock.Release(); }
        }

        public async Task EliminarConversacionAsync(int conversacionId)
        {
            await _lock.WaitAsync();
            try
            {
                string path = FilePath(conversacionId);
                if (File.Exists(path)) File.Delete(path);
            }
            finally { _lock.Release(); }
        }
    }

    public class MensajeGuardado
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public bool EsPropio { get; set; }
        public bool Leido { get; set; }
        public string Tipo { get; set; } = "Normal"; // "Normal" o "Sistema"
    }
}