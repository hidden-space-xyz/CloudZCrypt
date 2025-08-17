using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Extensions;

public static class MountPointExtensions
{
    /// <summary>
    /// Converts a MountPoint enum to a drive letter string (e.g., MountPoint.Z -> "Z:")
    /// </summary>
    public static string ToDriveString(this MountPoint mountPoint)
    {
        return $"{mountPoint}:";
    }

    /// <summary>
    /// Converts a drive letter string to a MountPoint enum (e.g., "Z:" -> MountPoint.Z)
    /// </summary>
    public static MountPoint ToMountPoint(this string driveString)
    {
        var driveLetter = driveString.Replace(":", "").ToUpperInvariant();
        return Enum.Parse<MountPoint>(driveLetter);
    }
}