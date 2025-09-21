using System.Globalization;
using System.Windows.Data;

namespace CloudZCrypt.WPF.Converters;

public class BoolToEyeEmojiConverter : IValueConverter
{
    public static readonly BoolToEyeEmojiConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isVisible && isVisible ? "🚫" : "👁️";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
