using System.Globalization;

namespace Mascotared.Converters
{
    /// <summary>
    /// Convierte un string que puede ser:
    ///  - URL normal (https://...)
    ///  - Base64 puro (sin prefijo)
    ///  - Data URI completo (data:image/jpeg;base64,...)
    /// en un ImageSource que MAUI puede mostrar directamente.
    /// </summary>
    public class Base64ToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string src || string.IsNullOrWhiteSpace(src))
                return null;

            try
            {
                // URL normal — dejar que MAUI la descargue
                if (src.StartsWith("http://") || src.StartsWith("https://"))
                    return ImageSource.FromUri(new Uri(src));

                // Extraer la parte base64 si viene con el prefijo data URI
                string base64 = src;
                if (src.StartsWith("data:"))
                {
                    int commaIndex = src.IndexOf(',');
                    if (commaIndex < 0) return null;
                    base64 = src[(commaIndex + 1)..];
                }

                // Limpiar espacios/saltos de línea que a veces añaden algunos encoders
                base64 = base64.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");

                byte[] bytes = System.Convert.FromBase64String(base64);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IMG-CONVERTER] Error al convertir imagen. InputStart='{src[..Math.Min(60, src.Length)]}' Error='{ex.Message}'");
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}