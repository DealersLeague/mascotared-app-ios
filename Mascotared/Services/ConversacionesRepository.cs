using System.Text.Json;
using Mascotared.Models;

namespace Mascotared.Services
{
    public class ConversacionesRepository
    {
        private static ConversacionesRepository? _instance;
        public static ConversacionesRepository Instance => _instance ??= new ConversacionesRepository();
        private ConversacionesRepository() { }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private List<ConversacionItem>? _cache;

        private string FilePath => Path.Combine(
            FileSystem.AppDataDirectory, "conversaciones.json");

        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // ── Leer local (siempre disponible, incluso offline) ──────────────
        public async Task<List<ConversacionItem>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_cache is not null) return _cache;
                if (!File.Exists(FilePath))
                {
                    _cache = new List<ConversacionItem>();
                    return _cache;
                }
                string json = await File.ReadAllTextAsync(FilePath);
                _cache = JsonSerializer.Deserialize<List<ConversacionItem>>(json, _opts)
                         ?? new List<ConversacionItem>();
                return _cache;
            }
            finally { _lock.Release(); }
        }

        // ── Guardar/actualizar una conversación localmente ────────────────
        public async Task GuardarOActualizarAsync(ConversacionItem conv)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                int idx = _cache.FindIndex(c => c.ContactoId == conv.ContactoId);
                if (idx >= 0) _cache[idx] = conv;
                else _cache.Add(conv);
                await PersistirAsync();
            }
            finally { _lock.Release(); }
        }

        // ── Actualizar último mensaje localmente ──────────────────────────
        public async Task ActualizarUltimoMensajeAsync(string contactoId, string texto, DateTime fecha)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                var conv = _cache.FirstOrDefault(c => c.ContactoId == contactoId);
                if (conv != null)
                {
                    conv.UltimoMensaje = texto;
                    conv.FechaUltimoMensaje = fecha;
                    conv.NoLeidos = 0; // los propios no generan badge
                    await PersistirAsync();
                }
            }
            finally { _lock.Release(); }
        }

        // ── Sincronizar con API: fusiona lo remoto en el caché local ──────
        // Llama a esto en background después de mostrar local.
        public async Task SincronizarDesdeApiAsync(List<ConversacionItem> remotas)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();

                foreach (var remota in remotas)
                {
                    int idx = _cache.FindIndex(c => c.ContactoId == remota.ContactoId);
                    if (idx >= 0)
                    {
                        var local = _cache[idx];
                        // Respetar archivado/eliminado local: no restaurar si el usuario lo ocultó
                        if (local.Archivada) continue;
                        // Actualizar solo campos que vienen frescos de la API
                        local.NombreContacto = remota.NombreContacto;
                        local.UltimoMensaje = remota.UltimoMensaje;
                        local.FechaUltimoMensaje = remota.FechaUltimoMensaje;
                        local.NoLeidos = remota.NoLeidos;
                        local.FotoContacto = remota.FotoContacto;
                    }
                    else
                    {
                        // Conversación nueva que no teníamos en local
                        _cache.Add(remota);
                    }
                }

                await PersistirAsync();
            }
            finally { _lock.Release(); }
        }

        // ── Eliminar conversación del local ───────────────────────────────
        public async Task EliminarAsync(string contactoId)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                _cache.RemoveAll(c => c.ContactoId == contactoId);
                await PersistirAsync();
            }
            finally { _lock.Release(); }
        }

        // ── Desarchivar conversación (vuelve a la lista principal) ───────
        public async Task DesarchivarAsync(string contactoId)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                var conv = _cache.FirstOrDefault(c => c.ContactoId == contactoId);
                if (conv != null)
                {
                    conv.Archivada = false;
                    await PersistirAsync();
                }
            }
            finally { _lock.Release(); }
        }

        // ── Archivar conversación (oculta sin borrar) ─────────────────────
        public async Task ArchivarAsync(string contactoId)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                var conv = _cache.FirstOrDefault(c => c.ContactoId == contactoId);
                if (conv != null)
                {
                    conv.Archivada = true;
                    await PersistirAsync();
                }
            }
            finally { _lock.Release(); }
        }

        // ── Marcar conversación como leída localmente ─────────────────────
        public async Task MarcarLeidaAsync(string contactoId)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<ConversacionItem>();
                var conv = _cache.FirstOrDefault(c => c.ContactoId == contactoId);
                if (conv != null)
                {
                    conv.NoLeidos = 0;
                    await PersistirAsync();
                }
            }
            finally { _lock.Release(); }
        }

        public void InvalidarCache() => _cache = null;

        private async Task PersistirAsync()
        {
            string json = JsonSerializer.Serialize(_cache, _opts);
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}