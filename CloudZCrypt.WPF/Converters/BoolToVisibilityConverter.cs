using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CloudZCrypt.WPF.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();
    public static readonly BoolToVisibilityConverter InstanceInverted = new() { IsInverted = true };

    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (IsInverted)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool result = visibility == Visibility.Visible;
            return IsInverted ? !result : result;
        }

        return false;
    }
}
