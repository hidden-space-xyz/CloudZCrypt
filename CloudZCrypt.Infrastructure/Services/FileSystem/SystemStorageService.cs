using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Provides access to low-level system storage information such as drive roots, available free space
/// and drive readiness status.
/// </summary>
/// <remarks>
/// This implementation is defensive: all methods catch and suppress any <see cref="System.Exception"/> that
/// might occur when querying the underlying file system / drive metadata APIs, returning a sentinel value instead
/// (null, -1, or false). This makes it safe to call these methods in higher-level workflows
/// (e.g. encryption pre-flight validation) without needing explicit exception handling for environmental issues
/// like missing drives, invalid paths, or insufficient permissions.
/// </remarks>
public class SystemStorageService : ISystemStorageService
{
    /// <summary>
    /// Gets the root portion (e.g. C:\) of the specified absolute path.
    /// </summary>
    /// <param name="fullPath">The full absolute file or directory path. May be malformed; if so, null is returned.</param>
    /// <returns>
    /// The root path (e.g. C:\) if it can be determined; otherwise null when the path is invalid
    /// or an error occurs while resolving the root.
    /// </returns>
    /// <remarks>
    /// Unlike the interface contract which allows for exceptions, this concrete implementation never throws; it returns null instead.
    /// </remarks>
    public string? GetPathRoot(string fullPath)
    {
        try
        {
            return Path.GetPathRoot(fullPath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the number of available free bytes for the specified drive/root path.
    /// </summary>
    /// <param name="rootPath">The drive or root directory path (e.g. C:\). May be invalid; if so, -1 is returned.</param>
    /// <returns>
    /// The number of available free bytes if the drive information can be read and the drive is ready; otherwise -1.
    /// </returns>
    /// <remarks>
    /// Any exception encountered (e.g. invalid path, inaccessible drive) results in the sentinel value -1.
    /// </remarks>
    public long GetAvailableFreeSpace(string rootPath)
    {
        try
        {
            DriveInfo driveInfo = new(rootPath);
            return driveInfo.IsReady ? driveInfo.AvailableFreeSpace : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Determines whether the specified drive/root path is ready (mounted and accessible).
    /// </summary>
    /// <param name="rootPath">The drive or root directory path to test (e.g. C:\). If invalid, false is returned.</param>
    /// <returns>true if the drive is ready; otherwise false (including when errors occur).</returns>
    /// <remarks>
    /// All exceptions are suppressed; an error condition yields false.
    /// </remarks>
    public bool IsDriveReady(string rootPath)
    {
        try
        {
            DriveInfo driveInfo = new(rootPath);
            return driveInfo.IsReady;
        }
        catch
        {
            return false;
        }
    }
}
