namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Provides an abstraction over common file system operations used by the application.
/// </summary>
/// <remarks>
/// Implementations should encapsulate all direct interactions with the file system to enable
/// easier testing, improved maintainability, and potential future redirection to virtual or
/// remote storage providers. All methods are expected to be safe for repeated calls and should
/// validate input arguments, throwing appropriate exceptions when invalid parameters are supplied.
/// </remarks>
public interface IFileOperationsService
{
    /// <summary>
    /// Asynchronously retrieves the full paths of files contained in the specified directory that match the given search pattern.
    /// </summary>
    /// <param name="directoryPath">The absolute or relative path of the directory to enumerate. Must not be <c>null</c> or empty.</param>
    /// <param name="searchPattern">An optional search pattern (e.g. "*.txt"). Defaults to <c>*.*</c> meaning all files.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An array of file paths matching the specified criteria. Returns an empty array if the directory exists but contains no matching files.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified <paramref name="directoryPath"/> does not exist.</exception>
    Task<string[]> GetFilesAsync(
        string directoryPath,
        string searchPattern = "*.*",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to check. Must not be <c>null</c> or empty.</param>
    /// <returns><c>true</c> if the directory exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is <c>null</c> or empty.</exception>
    bool DirectoryExists(string directoryPath);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="filePath">The full or relative path to the file. Must not be <c>null</c> or empty.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <c>null</c> or empty.</exception>
    bool FileExists(string filePath);

    /// <summary>
    /// Asynchronously creates the specified directory if it does not already exist.
    /// </summary>
    /// <param name="directoryPath">The path of the directory to create. Must not be <c>null</c> or empty.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is <c>null</c> or empty.</exception>
    Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size, in bytes, of the specified file.
    /// </summary>
    /// <param name="filePath">The full or relative path to the file. Must not be <c>null</c> or empty.</param>
    /// <returns>The size of the file in bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    long GetFileSize(string filePath);

    /// <summary>
    /// Computes a relative path from a base path to a target full path.
    /// </summary>
    /// <param name="basePath">The starting directory path. Must not be <c>null</c> or empty.</param>
    /// <param name="fullPath">The destination absolute path. Must not be <c>null</c> or empty.</param>
    /// <returns>The relative path from <paramref name="basePath"/> to <paramref name="fullPath"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePath"/> or <paramref name="fullPath"/> is <c>null</c> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a relative path cannot be computed.</exception>
    string GetRelativePath(string basePath, string fullPath);

    /// <summary>
    /// Combines the specified path segments into a single path using the correct directory separators for the current platform.
    /// </summary>
    /// <param name="paths">An ordered collection of path segments. Must not be <c>null</c> and must contain at least one element.</param>
    /// <returns>The combined path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="paths"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="paths"/> is empty or contains only whitespace elements.</exception>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Gets the directory portion of the specified file path.
    /// </summary>
    /// <param name="filePath">The full or relative path to a file or directory. Must not be <c>null</c> or empty.</param>
    /// <returns>The directory name, or <c>null</c> if the path denotes a root directory or cannot be resolved.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <c>null</c> or empty.</exception>
    string? GetDirectoryName(string filePath);
}
