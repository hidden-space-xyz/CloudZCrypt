using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Extensions;
using System.Globalization;
using System.Windows.Data;

namespace CloudZCrypt.WPF.Converters;

public class MountPointToDriveStringConverter : IValueConverter
{
    public static readonly MountPointToDriveStringConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is MountPoint mountPoint ? mountPoint.ToDriveString() : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string driveString && !string.IsNullOrWhiteSpace(driveString))
        {
            try
            {
                return driveString.ToMountPoint();
            }
            catch
            {
                return MountPoint.Z;
            }
        }
        return MountPoint.Z;
    }
}
