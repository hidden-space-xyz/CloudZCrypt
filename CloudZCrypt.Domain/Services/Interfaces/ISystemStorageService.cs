namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Defines a contract for querying low-level system storage information such as drive roots,
/// available free space, and drive readiness status.
/// </summary>
/// <remarks>
/// Implementations typically wrap <see cref="System.IO"/> / <see cref="System.Environment"/> APIs
/// to provide a testable abstraction for higher-level services that require disk space
/// validation (e.g., ensuring enough space before performing encryption operations).
/// </remarks>
public interface ISystemStorageService
{
    /// <summary>
    /// Gets the root component of the specified absolute path (e.g. C:\ for C:\dir\file.txt).
    /// </summary>
    /// <param name="fullPath">The absolute file or directory path from which to extract the root. Cannot be null, empty, or whitespace.</param>
    /// <returns>The root path (e.g. C:\) if it can be determined; otherwise null.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="fullPath"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="fullPath"/> is not a well-formed path.</exception>
    string? GetPathRoot(string fullPath);

    /// <summary>
    /// Gets the number of available free bytes for the specified drive/root path.
    /// </summary>
    /// <param name="rootPath">The drive or root directory path (e.g. C:\). Cannot be null, empty, or whitespace.</param>
    /// <returns>The number of free bytes available; returns -1 if the information cannot be determined.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="rootPath"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="rootPath"/> is not a well-formed root/drive path.</exception>
    long GetAvailableFreeSpace(string rootPath);

    /// <summary>
    /// Determines whether the specified drive/root path is ready (i.e. accessible and mounted).
    /// </summary>
    /// <param name="rootPath">The drive or root directory path to test (e.g. C:\). Cannot be null, empty, or whitespace.</param>
    /// <returns>true if the drive is ready; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="rootPath"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="rootPath"/> is not a well-formed root/drive path.</exception>
    bool IsDriveReady(string rootPath);
}
