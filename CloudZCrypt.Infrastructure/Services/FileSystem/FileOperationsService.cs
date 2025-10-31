using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

internal class FileOperationsService : IFileOperationsService
{
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

    public bool DirectoryExists(string directoryPath)
    {
        return Directory.Exists(directoryPath);
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public async Task CreateDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Run(() => Directory.CreateDirectory(directoryPath), cancellationToken);
    }

    public long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    public string GetRelativePath(string basePath, string fullPath)
    {
        return Path.GetRelativePath(basePath, fullPath);
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public string? GetDirectoryName(string filePath)
    {
        return Path.GetDirectoryName(filePath);
    }
}
