namespace CloudZCrypt.Domain.Services.Interfaces;

public interface ISystemStorageService
{
    // Returns the root path for a given full path (e.g., C:\ for C:\dir\file)
    string? GetPathRoot(string fullPath);

    // Returns available free space (in bytes) for a given root/drive path; returns -1 when not available
    long GetAvailableFreeSpace(string rootPath);

    // Indicates whether the given drive/root is ready
    bool IsDriveReady(string rootPath);
}
