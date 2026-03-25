using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mascotared.Services
{
    public class TaskItem
    {
        public int offerId { get; set; }
        // IDs privados, expuestos solo para lectura
        private string userId = string.Empty;
        private string helperId = string.Empty;
        private bool finished;
        private bool visible;
        public string AuthorId => userId;    // quien creó la tarea
        public string HelperId => helperId;  // quien fue aceptado
        public bool IsFinished                                   // ← ahora settable desde mapper
        {
            get => finished;
            set => finished = value;
        }
        public bool IsVisible => visible;    // si aparece en el listado público
        // Relación con el usuario actual
        public bool EsMia { get; set; }           // yo la creé
        public bool EsAceptado { get; set; }      // fui aceptado como cuidador
        public string? AceptadoPorId { get; set; }
        public string? UsuarioIdPublico { get; set; }  // id del autor (para reseñas)
        public string photoPath { get; set; } = string.Empty;
        // User data
        public string name { get; set; } = string.Empty;
        public int age { get; set; }
        public string userLocation { get; set; } = string.Empty;
        public Location offerLocation = new Location();
        public double distanceKm { get; set; }
        public int completedTasks { get; set; }
        // Datos del autor (foto, valoración, localización de perfil)
        public string? FotoAutor { get; set; }
        public decimal ValoracionAutor { get; set; }
        public string? LocAutor { get; set; }
        // Coordenadas
        public double? latitud { get; set; }
        public double? longitud { get; set; }
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public List<string> tags { get; set; } = new();
        public int favorites;
        public bool caretaker { get; set; }
        // Tiempo
        public List<string> weekDays { get; set; } = new();
        public List<string> timeOfDay { get; set; } = new();
        // Precio
        public decimal perHour { get; set; }
        public DateTime? date0 { get; set; }
        public DateTime? date1 { get; set; }
        public bool exactHours { get; set; }
        public TimeSpan? timeSpan0 { get; set; }
        public TimeSpan? timeSpan1 { get; set; }
        public decimal totalPrice { get; set; }
        // Mascotas
        public List<string> petList { get; set; } = new();
        public bool hadPets { get; set; }
        public int maxPets { get; set; }
        public bool specialNeeds { get; set; }
        public string specialNeedsDes { get; set; } = string.Empty;
        public string specialNeedsXP { get; set; } = string.Empty;
        public List<string> canLookafter { get; set; } = new();
        public bool editable { get; set; }
        // ── Constructor vacío (deserializador JSON) ──────────────────────────
        public TaskItem() { }
        // ── Constructor cuidador ─────────────────────────────────────────────
        public TaskItem(bool _caretaker, string _photoPath, string _title, string _description,
            List<string> _tags, List<string> _weekDays, List<string> _timeOfDay,
            bool _hadPets, int _maxPets, bool _specialNeeds, string _specialNeedsXP,
            List<string> _canLookAfter)
        {
            editable = true;
            visible = true;
            finished = false;
            caretaker = _caretaker;
            userId = Preferences.Get("user_id", string.Empty);
            age = UsuarioService.Instancia.Usuario.Edad;
            userLocation = UsuarioService.Instancia.Usuario.Localizacion;
            perHour = UsuarioService.Instancia.Usuario.TarifaPorHora;
            title = _title;
            description = _description;
            tags = _tags;
            favorites = 0;
            weekDays = _weekDays;
            timeOfDay = _timeOfDay;
            hadPets = _hadPets;
            maxPets = _maxPets;
            specialNeeds = _specialNeeds;
            specialNeedsXP = _specialNeedsXP;
            canLookafter = _canLookAfter;
        }
        // ── Constructor propietario ──────────────────────────────────────────
        public TaskItem(bool _caretaker, string _photoPath, string _title, string _description,
            List<string> _tags, decimal _totalPrice, DateTime? _date0, DateTime? _date1,
            bool _exactHours, TimeSpan? _timeSpan0, TimeSpan? _timeSpan1,
            List<string> _timeOfDay, bool _specialNeeds, string _specialNeedsDes,
            List<string> _petList)
        {
            editable = true;
            visible = true;
            finished = false;
            caretaker = _caretaker;
            userId = Preferences.Get("user_id", string.Empty);
            photoPath = _photoPath;
            age = UsuarioService.Instancia.Usuario.Edad;
            userLocation = UsuarioService.Instancia.Usuario.Localizacion;
            title = _title;
            description = _description;
            tags = _tags;
            totalPrice = _totalPrice;
            favorites = 0;
            date0 = _date0;
            date1 = _date1;
            exactHours = _exactHours;
            if (_exactHours) { timeSpan0 = _timeSpan0; timeSpan1 = _timeSpan1; }
            else { timeOfDay = _timeOfDay; }
            specialNeeds = _specialNeeds;
            specialNeedsDes = _specialNeedsDes;
            petList = _petList;
        }
        // ── Acciones ─────────────────────────────────────────────────────────
        /// El autor acepta a un solicitante: la tarea deja de ser pública
        public void OfferAceppted(string _helperId)
        {
            helperId = _helperId;
            editable = false;
            visible = false;
        }

        public void ResetData()
        {
            offerId = 0;
            helperId = string.Empty;
            editable = true;
            visible = true;
        }

        /// El autor marca la tarea como completada
        public void Finalizar() => finished = true;
        /// Alias para compatibilidad con código existente
        public void Archived() => finished = true;
        public double DistanceCalculator(Location? userLoc)
            => distanceKm = Location.CalculateDistance(offerLocation, userLoc, DistanceUnits.Kilometers);
    }
    // ── Modelo para solicitudes enviadas a ofertas de otros ──────────────────
    public class SolicitudItem
    {
        public int Id { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public int OfertaId { get; set; }
        public bool OfertaEsCuidador { get; set; }
        public string OfertaTitulo { get; set; } = string.Empty;
        public string OfertaDescripcion { get; set; } = string.Empty;
        public string? OfertaLocalizacion { get; set; }
        public decimal? OfertaTarifaPorHora { get; set; }
        public decimal? OfertaPrecioTotal { get; set; }
        public bool OfertaFinalizada { get; set; }
        public string AutorNombre { get; set; } = string.Empty;
        public string? AutorFoto { get; set; }
        public string? AutorId { get; set; }
    }
    // ── Modelo para solicitudes RECIBIDAS en mis ofertas ─────────────────────
    public class SolicitudRecibidaItem
    {
        public int Id { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Mensaje { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string SolicitanteId { get; set; } = string.Empty;
        public string SolicitanteNombre { get; set; } = string.Empty;
        public string? SolicitanteFoto { get; set; }
        public int OfertaId { get; set; }
        public string OfertaTitulo { get; set; } = string.Empty;
        public bool OfertaFinalizada { get; set; }
    }

    public class TaskDupp
    {
        ApiService api;
        string userId = string.Empty;

        public TaskDupp()
        {
            api = new ApiService();
            userId = Preferences.Get("user_id", string.Empty);
        }

        public async Task GetNewTaskId(int _offerId)
        {
            var tarea = await api.GetOfertaByIdAsync(_offerId);
            if(tarea != null)
            {
                tarea.ResetData();
                await TasksRepository.Instance.SaveOrUpdateAsync(tarea);
            }
        }
    }
}