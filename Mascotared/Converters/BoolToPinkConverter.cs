using System.Globalization;

namespace Mascotared;

public class BoolToPinkConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esLikeado && esLikeado)
            return Color.FromArgb("#FE3D7D"); // Pink
        return Color.FromArgb("#9AA0C4"); // Mid (gris)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
