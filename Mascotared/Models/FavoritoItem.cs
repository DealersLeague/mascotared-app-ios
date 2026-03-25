using System.Text.Json.Serialization;

namespace Mascotared.Models
{
    public class FavoritoItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("cuidadorId")] public string CuidadorId { get; set; } = string.Empty;
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("foto")] public string? Foto { get; set; }
        [JsonPropertyName("tarifa")] public decimal? Tarifa { get; set; }
        [JsonPropertyName("valoracion")] public double? Valoracion { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
        [JsonPropertyName("direccion")] public string? Direccion { get; set; }

        // Calculados para la UI
        public string TarifaTexto => Tarifa.HasValue ? $"{Tarifa:F2} €/h" : "";
        public string ValoracionTexto => Valoracion.HasValue && Valoracion.Value > 0
            ? $"⭐ {Valoracion.Value:F1}" : "";
        public string Inicial => Nombre.Length > 0 ? Nombre[0].ToString().ToUpper() : "?";

        // Texto unificado de los tags para binding XAML
        public string TagsTexto => Tags != null && Tags.Count > 0
            ? string.Join(" · ", Tags) : "";

        // Indica si hay foto de perfil
        public bool TieneFoto => !string.IsNullOrEmpty(Foto);

        // ImageSource para binding XAML (maneja URL, base64 y data URI)
        public ImageSource? FotoSource
        {
            get
            {
                if (string.IsNullOrEmpty(Foto)) return null;
                try
                {
                    if (Foto.StartsWith("http://") || Foto.StartsWith("https://"))
                        return ImageSource.FromUri(new Uri(Foto));
                    string b64 = Foto.Contains(',')
                        ? Foto[(Foto.IndexOf(',') + 1)..] : Foto;
                    b64 = b64.Trim().Replace("\n", "").Replace("\r", "");
                    var bytes = Convert.FromBase64String(b64);
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }
                catch { return null; }
            }
        }

        // true si es cuidador (para mostrar tarifa)
        public bool EsCuidador => Tags?.Any(t =>
            t.Contains("Cuidador", StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
