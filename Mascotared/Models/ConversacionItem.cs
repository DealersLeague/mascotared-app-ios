namespace Mascotared.Models
{
    public class ConversacionItem
    {
        // ── Identificación ────────────────────────────────────────────────
        /// <summary>UserId del otro usuario — clave para llamadas API</summary>
        public string ContactoId { get; set; } = string.Empty;
        /// <summary>Compatibilidad con código antiguo. Usa ContactoId para llamadas API.</summary>
        public int Id => string.IsNullOrEmpty(ContactoId) ? 0 : Math.Abs(ContactoId.GetHashCode());
        public string NombreContacto { get; set; } = string.Empty;
        public string? FotoPath { get; set; }

        // ── Foto de perfil del contacto (base64 o URL) ───────────────────
        public string? FotoContacto { get; set; }
        public bool TieneFoto => !string.IsNullOrEmpty(FotoContacto);
        public bool NoTieneFoto => string.IsNullOrEmpty(FotoContacto);
        public ImageSource? FotoSource
        {
            get
            {
                if (string.IsNullOrEmpty(FotoContacto)) return null;
                try
                {
                    // URL directa
                    if (FotoContacto.StartsWith("http://") || FotoContacto.StartsWith("https://"))
                        return ImageSource.FromUri(new Uri(FotoContacto));
                    // Base64 (con o sin cabecera data:...)
                    string b64 = FotoContacto.Contains(',')
                        ? FotoContacto[(FotoContacto.IndexOf(',') + 1)..] : FotoContacto;
                    b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                    var bytes = Convert.FromBase64String(b64);
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch { return null; }
            }
        }

        // ── Archivado/eliminado localmente ───────────────────────────────
        public bool Archivada { get; set; }

        // ── Último mensaje ────────────────────────────────────────────────
        public string UltimoMensaje { get; set; } = string.Empty;
        public DateTime FechaUltimoMensaje { get; set; }
        public int NoLeidos { get; set; }
        public string? MascotaRelacionada { get; set; }
        // ── Solicitud relacionada ─────────────────────────────────────────
        public int? SolicitudId { get; set; }
        public int? OfertaId { get; set; }
        /// <summary>"Pendiente", "Aceptada", "Rechazada" o null</summary>
        public string? EstadoSolicitud { get; set; }
        /// <summary>True si el usuario actual es el autor de la oferta</summary>
        public bool SoyPropietario { get; set; }
        // ── Datos para cabecera del chat ──────────────────────────────────
        public string? TituloSolicitud { get; set; }
        public string? DetalleSolicitud { get; set; }
        public string Valoracion { get; set; } = "Sin valoraciones";
        public string TiempoRespuesta { get; set; } = "Activo: Recientemente";
        // ── Propiedades calculadas ────────────────────────────────────────
        public string Inicial => string.IsNullOrEmpty(NombreContacto) ? "?"
            : NombreContacto.Trim()[0].ToString().ToUpper();
        public string ColorAvatar =>
            _colores[Math.Abs(NombreContacto.GetHashCode()) % _colores.Length];
        public bool TieneNoLeidos => NoLeidos > 0;
        public bool MostrarContador => NoLeidos > 0;
        public string ContadorNoLeidos => NoLeidos > 9 ? "9+" : NoLeidos.ToString();
        public bool TieneMascota => !string.IsNullOrEmpty(MascotaRelacionada);
        public string EtiquetaMascota => MascotaRelacionada ?? string.Empty;
        public string HoraFormateada
        {
            get
            {
                var diff = DateTime.Now - FechaUltimoMensaje;
                if (diff.TotalMinutes < 1) return "ahora";
                if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m";
                if (diff.TotalDays < 1) return FechaUltimoMensaje.ToString("HH:mm");
                if (diff.TotalDays < 7) return FechaUltimoMensaje.ToString("ddd");
                return FechaUltimoMensaje.ToString("dd/MM");
            }
        }
        private static readonly string[] _colores =
        {
            "#455AEB", "#FE3D7D", "#22AABB", "#F59E0B",
            "#10B981", "#8B5CF6", "#EF4444", "#F97316"
        };
    }
}