using System.Globalization;
using System.Windows.Data;

namespace CloudZCrypt.WPF.Converters;

/// <summary>
/// Converts a boolean value indicating visibility into a corresponding eye-themed emoji for UI presentation.
/// </summary>
/// <remarks>
/// This converter is typically used in password reveal / conceal toggles. When the bound boolean value is true,
/// it returns the blocked eye emoji (🚫) to suggest that clicking will hide the currently visible content. When the value
/// is false, it returns the standard eye emoji (👁️) to suggest that clicking will reveal hidden content.
/// </remarks>
public class BoolToEyeEmojiConverter : IValueConverter
{
    /// <summary>
    /// Gets a shared singleton instance of the <see cref="BoolToEyeEmojiConverter"/> for convenient reuse in XAML resources.
    /// </summary>
    public static readonly BoolToEyeEmojiConverter Instance = new();

    /// <summary>
    /// Converts a boolean value representing visibility into an eye-related emoji string suitable for display.
    /// </summary>
    /// <param name="value">The source value provided by the binding; expected to be a <see cref="bool"/>. Non-boolean values result in the default (👁️) emoji.</param>
    /// <param name="targetType">The type of the binding target property. Not used.</param>
    /// <param name="parameter">An optional parameter supplied by the binding. Not used.</param>
    /// <param name="culture">The culture to use in the converter. Not used.</param>
    /// <returns>
    /// Returns the string "🚫" when <paramref name="value"/> is a boolean true; otherwise returns "👁️".
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isVisible && isVisible ? "🚫" : "👁️";
    }

    /// <summary>
    /// Not supported. Converts back from an emoji string to a boolean value.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target. Ignored.</param>
    /// <param name="targetType">The type to convert to. Ignored.</param>
    /// <param name="parameter">An optional parameter supplied by the binding. Ignored.</param>
    /// <param name="culture">The culture to use in the converter. Ignored.</param>
    /// <returns>This method does not return; it always throws.</returns>
    /// <exception cref="NotImplementedException">Always thrown since reverse conversion is not implemented.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
