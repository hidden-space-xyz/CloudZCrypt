namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Domain service interface for basic file operations
/// Abstracts file system operations from the domain logic
/// </summary>
public interface IFileOperationsService
{
    /// <summary>
    /// Gets all files in a directory recursively
    /// </summary>
    Task<string[]> GetFilesAsync(string directoryPath, string searchPattern = "*.*", CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory exists
    /// </summary>
    bool DirectoryExists(string directoryPath);

    /// <summary>
    /// Creates a directory if it doesn't exist
    /// </summary>
    Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size of a file in bytes
    /// </summary>
    long GetFileSize(string filePath);

    /// <summary>
    /// Gets the relative path between two paths
    /// </summary>
    string GetRelativePath(string basePath, string fullPath);

    /// <summary>
    /// Combines path segments
    /// </summary>
    string CombinePath(params string[] paths);

    /// <summary>
    /// Gets the directory name from a file path
    /// </summary>
    string? GetDirectoryName(string filePath);
}