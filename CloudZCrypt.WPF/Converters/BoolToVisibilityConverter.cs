using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CloudZCrypt.WPF.Converters;

/// <summary>
/// Converts <see cref="bool"/> values to <see cref="Visibility"/> values for WPF data binding scenarios.
/// </summary>
/// <remarks>
/// This converter maps true to <see cref="Visibility.Visible"/> and false to <see cref="Visibility.Collapsed"/> by default.
/// The mapping can be inverted by using the <see cref="InstanceInverted"/> singleton or by setting <see cref="IsInverted"/> to true.
/// </remarks>
/// <seealso cref="IValueConverter"/>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets a shared singleton instance of the converter that maps true to <see cref="Visibility.Visible"/>.
    /// </summary>
    public static readonly BoolToVisibilityConverter Instance = new();

    /// <summary>
    /// Gets a shared singleton instance of the converter that maps true to <see cref="Visibility.Collapsed"/>,
    /// effectively inverting the standard mapping.
    /// </summary>
    public static readonly BoolToVisibilityConverter InstanceInverted = new() { IsInverted = true };

    /// <summary>
    /// Gets or sets a value indicating whether the boolean-to-visibility mapping is inverted.
    /// When true, true maps to <see cref="Visibility.Collapsed"/> and false maps to <see cref="Visibility.Visible"/>.
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// Converts a <see cref="bool"/> value (or compatible object) to a <see cref="Visibility"/> value, optionally applying inversion.
    /// </summary>
    /// <param name="value">The source value produced by the binding source. Expected to be a <see cref="bool"/>.</param>
    /// <param name="targetType">The type of the binding target property. This parameter is not used.</param>
    /// <param name="parameter">An optional parameter supplied by the binding. This implementation ignores it.</param>
    /// <param name="culture">The culture to use in the converter. This implementation ignores it.</param>
    /// <returns>
    /// <see cref="Visibility.Visible"/> when the (possibly inverted) boolean value is true; otherwise <see cref="Visibility.Collapsed"/>.
    /// Returns <see cref="Visibility.Collapsed"/> if <paramref name="value"/> is not a <see cref="bool"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (IsInverted)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <summary>
    /// Converts a <see cref="Visibility"/> value back to a <see cref="bool"/>, optionally applying inversion.
    /// </summary>
    /// <param name="value">The source value (expected to be a <see cref="Visibility"/>).</param>
    /// <param name="targetType">The type of the binding target property. This parameter is not used.</param>
    /// <param name="parameter">An optional parameter supplied by the binding. This implementation ignores it.</param>
    /// <param name="culture">The culture to use in the converter. This implementation ignores it.</param>
    /// <returns>
    /// true if the (possibly inverted) visibility is <see cref="Visibility.Visible"/>; otherwise false.
    /// Returns false if <paramref name="value"/> is not a <see cref="Visibility"/>.
    /// </returns>
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
