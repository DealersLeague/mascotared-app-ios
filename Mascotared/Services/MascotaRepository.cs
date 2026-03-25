using System.Text.Json;
using Mascotared.Models;

namespace Mascotared.Services
{
    public class MascotaRepository
    {
        private static MascotaRepository? _instance;
        public static MascotaRepository Instance => _instance ??= new MascotaRepository();
        private MascotaRepository() { }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private List<MascotasItem>? _cache;

        private string FilePath => Path.Combine(
            FileSystem.AppDataDirectory, "mascotas.json");

        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<MascotasItem>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_cache is not null) return _cache;
                if (!File.Exists(FilePath))
                {
                    _cache = new List<MascotasItem>();
                    return _cache;
                }
                string json = await File.ReadAllTextAsync(FilePath);
                _cache = JsonSerializer.Deserialize<List<MascotasItem>>(json, _opts)
                         ?? new List<MascotasItem>();
                return _cache;
            }
            finally { _lock.Release(); }
        }

        public async Task SaveOrUpdateAsync(MascotasItem mascota)
        {
            await _lock.WaitAsync();
            try
            {
                _cache ??= new List<MascotasItem>();
                if (mascota.Id == 0)
                {
                    mascota.Id = _cache.Count > 0 ? _cache.Max(m => m.Id) + 1 : 1;
                    _cache.Add(mascota);
                }
                else
                {
                    int idx = _cache.FindIndex(m => m.Id == mascota.Id);
                    if (idx >= 0) _cache[idx] = mascota;
                    else _cache.Add(mascota);
                }
                await PersistAsync();
            }
            finally { _lock.Release(); }
        }

        public async Task DeleteAsync(int id)
        {
            await _lock.WaitAsync();
            try
            {
                _cache?.RemoveAll(m => m.Id == id);
                await PersistAsync();
            }
            finally { _lock.Release(); }
        }

        private async Task PersistAsync()
        {
            string json = JsonSerializer.Serialize(_cache, _opts);
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}