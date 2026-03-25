using System;
namespace Mascotared.Models
{
    public class MascotasItem
    {
        public int Id { get; set; }
        public int ApiId { get; set; } = 0;  // ID asignado por el backend (0 = aún no subida)
        public string UsuarioId { get; set; } = string.Empty;


        // ── Información Básica ──────────────────────────────────
        public string PhotoPath { get; set; } = string.Empty;
        public string Especie { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Race { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateTime? FechaNacimiento { get; set; }
        public int? EdadAnios { get; set; }
        public int? EdadMeses { get; set; }
        public decimal? Peso { get; set; }
        public string UnidadPeso { get; set; } = "kg";

        // ── Identificación ──────────────────────────────────────
        public string? Microchip { get; set; }
        public string? NumeroAnilla { get; set; }
        public string? CITES { get; set; }
        public string? EstadoReproductivo { get; set; }
        public string? ColorMarcas { get; set; }

        // ── Salud y Cuidados Críticos ───────────────────────────
        public string? Alergias { get; set; }               // "Alérgico al pollo, sin granos"
        public string? CondicionesMedicas { get; set; }     // "Diabetes, Epilepsia..."
        public string? NivelMovilidad { get; set; }         // "Normal", "Requiere rampa", "No puede subir escaleras"
        public string? TipoMedicacion { get; set; }
        public string? HorarioMedicacion { get; set; }
        public bool TieneCuidados => !string.IsNullOrEmpty(TipoMedicacion)
                                  || !string.IsNullOrEmpty(CondicionesMedicas)
                                  || !string.IsNullOrEmpty(Alergias);
        public string? CuidadosEspeciales => CondicionesMedicas ?? Alergias ?? TipoMedicacion;

        // ── Comportamiento y Paseo ──────────────────────────────
        public string? MiedosReactividad { get; set; }      // "Truenos, motos, bicicletas"
        public string? TipoCorreaArnes { get; set; }        // "Collar normal", "Arnés antitirones", "Collar de cabeza"
        public bool? InstintoPresa { get; set; }
        public bool? BienConNinos { get; set; }
        public bool? BienConMismaEspecie { get; set; }
        public bool? BienConOtrasEspecies { get; set; }

        // ── Personalidad (tags) ─────────────────────────────────
        public string? Tags { get; set; }                   // "Miedoso,Glotón,Ladrador"
        public List<string> ListaTags => Tags?.Split(',',
            StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();

        // ── Alimentación ────────────────────────────────────────
        public string? NivelEnergia { get; set; }
        public string? FrecuenciaAlimentacion { get; set; }
        public string? AlimentosProhibidos { get; set; }

        // ── Veterinario ─────────────────────────────────────────
        public string? VeterinarioNombre { get; set; }
        public string? VeterinarioDireccion { get; set; }
        public string? VeterinarioTelefono { get; set; }

        // ── Protocolo de Emergencia ─────────────────────────────
        public string? CentroUrgencias { get; set; }
        public string? TelefonoUrgencias { get; set; }
        public string? PresupuestoEmergencia { get; set; }  // "Hasta 500€", "Sin límite"

        // ── Documentación ───────────────────────────────────────
        public string? DocumentoPath { get; set; }
        public bool TieneSeguroRC { get; set; }
        public bool TieneVacunasAlDia { get; set; }
        public string? FotoCarnetVacunas { get; set; }

        // ── Específico: Aves ────────────────────────────────────
        public string? TipoVuelo { get; set; }
        public string? CantosOPalabras { get; set; }
        public string? HumedadNecesaria { get; set; }
        public DateTime? UltimaMuda { get; set; }
        public decimal? HorasLuzUV { get; set; }

        // ── Específico: Reptiles ────────────────────────────────
        public string? TipoHabitat { get; set; }
        public string? RangoTemperaturaDia { get; set; }
        public string? RangoTemperaturaNoche { get; set; }

        // ── Específico: Caballos ────────────────────────────────
        public string? Capa { get; set; }
        public string? HierroGanaderia { get; set; }
        public string? Uso { get; set; }
        public decimal? Alzada { get; set; }
        public string? EstadoHerrado { get; set; }

        // ── Específico: Pequeños mamíferos ──────────────────────
        public string? TipoSustrato { get; set; }
        public decimal? TiempoFueraJaula { get; set; }
        public string? TipoArena { get; set; }              // Gatos

        // ── Notas ───────────────────────────────────────────────
        public string? Description { get; set; }
    }
}