using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Models;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Application.Services;

/// <summary>
/// Application service for virtual file system operations
/// </summary>
internal class VirtualFileSystemApplicationService : IVirtualFileSystemApplicationService
{
    private readonly IVirtualFileSystemService _virtualFileSystemService;

    public VirtualFileSystemApplicationService(IVirtualFileSystemService virtualFileSystemService)
    {
        _virtualFileSystemService = virtualFileSystemService;
    }

    public async Task<Result<bool>> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(encryptedDirectoryPath))
                return Result<bool>.Failure(["Encrypted directory path is required"]);

            if (string.IsNullOrWhiteSpace(mountPoint))
                return Result<bool>.Failure(["Mount point is required"]);

            if (string.IsNullOrWhiteSpace(password))
                return Result<bool>.Failure(["Password is required"]);

            if (!Directory.Exists(encryptedDirectoryPath))
                return Result<bool>.Failure(["Encrypted directory does not exist"]);

            // Check if mount point is already in use
            if (_virtualFileSystemService.IsMounted(mountPoint))
                return Result<bool>.Failure(["Mount point is already in use"]);

            var success = await _virtualFileSystemService.MountVolumeAsync(
                encryptedDirectoryPath,
                mountPoint,
                password,
                encryptionAlgorithm,
                keyDerivationAlgorithm);

            if (!success)
                return Result<bool>.Failure(["Failed to mount volume"]);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure([$"Error mounting volume: {ex.Message}"]);
        }
    }

    public async Task<Result<bool>> UnmountVolumeAsync(string mountPoint)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mountPoint))
                return Result<bool>.Failure(["Mount point is required"]);

            if (!_virtualFileSystemService.IsMounted(mountPoint))
                return Result<bool>.Failure(["Mount point is not mounted"]);

            var success = await _virtualFileSystemService.UnmountVolumeAsync(mountPoint);

            if (!success)
                return Result<bool>.Failure(["Failed to unmount volume"]);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure([$"Error unmounting volume: {ex.Message}"]);
        }
    }

    public async Task<Result<IEnumerable<MountedVolume>>> GetMountedVolumesAsync()
    {
        try
        {
            var mountedVolumes = _virtualFileSystemService.GetMountedVolumes()
                .Select(mp => new MountedVolume
                {
                    MountPoint = mp,
                    EncryptedDirectoryPath = "Unknown", // This would need to be tracked
                    MountedAt = DateTime.UtcNow, // This would need to be tracked
                    IsMounted = true
                });

            return Result<IEnumerable<MountedVolume>>.Success(mountedVolumes);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MountedVolume>>.Failure([$"Error getting mounted volumes: {ex.Message}"]);
        }
    }

    public async Task<Result<bool>> IsMountPointAvailableAsync(string mountPoint)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mountPoint))
                return Result<bool>.Failure(["Mount point is required"]);

            var isAvailable = !_virtualFileSystemService.IsMounted(mountPoint);
            return Result<bool>.Success(isAvailable);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure([$"Error checking mount point availability: {ex.Message}"]);
        }
    }
}