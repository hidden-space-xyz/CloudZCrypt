using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Provides file system operations such as enumeration, existence checks, directory creation,
/// path manipulation, and file metadata retrieval for the application.
/// </summary>
/// <remarks>
/// This service is an implementation of <see cref="IFileOperationsService"/> that directly leverages
/// the <see cref="System.IO"/> APIs. It centralizes all file system access to facilitate testing and
/// future redirection to virtualized or remote storage layers. Methods are asynchronous where I/O
/// could be potentially long running. Input parameters are expected to be valid; callers should
/// ensure arguments are not null or empty unless otherwise noted.
/// </remarks>
public class FileOperationsService : IFileOperationsService
{
    /// <summary>
    /// Asynchronously gets all files under the specified directory (recursively) that match the provided search pattern.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path of the directory to search.</param>
    /// <param name="searchPattern">An optional search pattern (e.g. "*.txt"). Defaults to *.* to include all files.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>An array containing the full paths of all matching files. Returns an empty array if no matches are found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath"/> is an empty string, contains invalid characters, or <paramref name="searchPattern"/> is invalid.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified <paramref name="directoryPath"/> does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task<string[]> GetFilesAsync(
        string directoryPath,
        string searchPattern = "*.*",
        CancellationToken cancellationToken = default
    )
    {
        return await Task.Run(
            () => Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories),
            cancellationToken
        );
    }

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="directoryPath">The path of the directory to check.</param>
    /// <returns>true if the directory exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is null or empty.</exception>
    public bool DirectoryExists(string directoryPath)
    {
        return Directory.Exists(directoryPath);
    }

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="filePath">The full or relative path of the file to check.</param>
    /// <returns>true if the file exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// Asynchronously creates the specified directory if it does not already exist.
    /// </summary>
    /// <param name="directoryPath">The path of the directory to create.</param>
    /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous directory creation operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is null or empty.</exception>
    /// <exception cref="IOException">Thrown when the directory cannot be created due to an I/O error.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have the required permission.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    public async Task CreateDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Run(() => Directory.CreateDirectory(directoryPath), cancellationToken);
    }

    /// <summary>
    /// Gets the size (in bytes) of the specified file.
    /// </summary>
    /// <param name="filePath">The full or relative path of the file whose size should be retrieved.</param>
    /// <returns>The size of the file in bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    /// <summary>
    /// Computes the relative path from a base path to a target full path.
    /// </summary>
    /// <param name="basePath">The starting directory path.</param>
    /// <param name="fullPath">The destination full (absolute) path.</param>
    /// <returns>The relative path from <paramref name="basePath"/> to <paramref name="fullPath"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePath"/> or <paramref name="fullPath"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a relative path cannot be determined.</exception>
    public string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }

    /// <summary>
    /// Combines multiple path segments into a single path using platform-appropriate directory separators.
    /// </summary>
    /// <param name="paths">One or more path segments to combine.</param>
    /// <returns>The combined path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="paths"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="paths"/> is empty or all elements are whitespace.</exception>
    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <summary>
    /// Gets the directory component of the specified file or directory path.
    /// </summary>
    /// <param name="filePath">The path from which to obtain the directory component.</param>
    /// <returns>The directory name, or null if the path denotes a root or cannot be resolved.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    public string? GetDirectoryName(string filePath)
    {
        return Path.GetDirectoryName(filePath);
    }
}
