using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Provides access to low-level system storage information such as drive roots, available free space
/// and drive readiness status.
/// </summary>
/// <remarks>
/// This implementation is defensive: all methods catch and suppress any <see cref="System.Exception"/> that
/// might occur when querying the underlying file system / drive metadata APIs, returning a sentinel value instead
/// (<c>null</c>, <c>-1</c>, or <c>false</c>). This makes it safe to call these methods in higher-level workflows
/// (e.g. encryption pre-flight validation) without needing explicit exception handling for environmental issues
/// like missing drives, invalid paths, or insufficient permissions.
/// </remarks>
public class SystemStorageService : ISystemStorageService
{
    /// <summary>
    /// Gets the root portion (e.g. <c>C:\</c>) of the specified absolute path.
    /// </summary>
    /// <param name="fullPath">The full absolute file or directory path. May be malformed; if so, <c>null</c> is returned.</param>
    /// <returns>
    /// The root path (e.g. <c>C:\</c>) if it can be determined; otherwise <c>null</c> when the path is invalid
    /// or an error occurs while resolving the root.
    /// </returns>
    /// <remarks>
    /// Unlike the interface contract which allows for exceptions, this concrete implementation never throws; it returns <c>null</c> instead.
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
    /// <param name="rootPath">The drive or root directory path (e.g. <c>C:\</c>). May be invalid; if so, <c>-1</c> is returned.</param>
    /// <returns>
    /// The number of available free bytes if the drive information can be read and the drive is ready; otherwise <c>-1</c>.
    /// </returns>
    /// <remarks>
    /// Any exception encountered (e.g. invalid path, inaccessible drive) results in the sentinel value <c>-1</c>.
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
    /// <param name="rootPath">The drive or root directory path to test (e.g. <c>C:\</c>). If invalid, <c>false</c> is returned.</param>
    /// <returns><c>true</c> if the drive is ready; otherwise <c>false</c> (including when errors occur).</returns>
    /// <remarks>
    /// All exceptions are suppressed; an error condition yields <c>false</c>.
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
