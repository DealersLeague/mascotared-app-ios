using System.Text.Json;
using Mascotared.Models;
using Mascotared.Services;

namespace Mascotared.Services
{
    public class TasksRepository
    {
        private static TasksRepository? _instance;
        public static TasksRepository Instance => _instance ??= new TasksRepository();
        private TasksRepository() { }

        private readonly ApiService _api = new();

        // ── Leer todas las ofertas desde la API ───────────────────────────────
        // esCuidador: true → solo cuidadores, false → solo propietarios, null → todas
        public async Task<List<TaskItem>> GetAllAsync(bool? esCuidador = null)
        {
            try
            {
                return await _api.GetOfertasAsync(esCuidador);
            }
            catch
            {
                return new List<TaskItem>();
            }
        }

        // ── Guardar o actualizar una oferta en la API ─────────────────────────
        public async Task SaveOrUpdateAsync(TaskItem task)
        {
            try
            {
                if (task.offerId == 0)
                    await _api.CrearOfertaAsync(task);
                else
                    await _api.ActualizarOfertaAsync(task.offerId, task);
            }
            catch { /* fallo silencioso, se reintentará */ }
        }

        // ── Eliminar una oferta de la API ─────────────────────────────────────
        public async Task DeleteAsync(int id)
        {
            try
            {
                await _api.EliminarOfertaAsync(id);
            }
            catch { }
        }
    }
}