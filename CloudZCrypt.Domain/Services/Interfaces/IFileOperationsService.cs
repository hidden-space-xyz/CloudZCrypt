namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IFileOperationsService
{
    Task<string[]> GetFilesAsync(
        string directoryPath,
        string searchPattern = "*.*",
        CancellationToken cancellationToken = default
    );
    bool DirectoryExists(string directoryPath);
    bool FileExists(string filePath);
    Task CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    long GetFileSize(string filePath);
    string GetRelativePath(string basePath, string fullPath);
    string CombinePath(params string[] paths);
    string? GetDirectoryName(string filePath);
}
