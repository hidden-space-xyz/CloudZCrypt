using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Extensions;

public static class MountPointExtensions
{
    public static string ToDriveString(this MountPoint mountPoint)
    {
        return $"{mountPoint}:";
    }
    public static MountPoint ToMountPoint(this string driveString)
    {
        string driveLetter = driveString.Replace(":", "").ToUpperInvariant();
        return Enum.Parse<MountPoint>(driveLetter);
    }
}
