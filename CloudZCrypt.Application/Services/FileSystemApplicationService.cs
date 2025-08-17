using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Application.Services;
internal class FileSystemApplicationService(IFileSystemService virtualFileSystemService) : IFileSystemApplicationService
{
    public async Task<Result<bool>> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(encryptedDirectoryPath))
                return Result<bool>.Failure(["Encrypted directory path is required"]);

            if (string.IsNullOrWhiteSpace(mountPoint))
                return Result<bool>.Failure(["Mount point is required"]);

            if (string.IsNullOrWhiteSpace(password))
                return Result<bool>.Failure(["Password is required"]);

            if (!Directory.Exists(encryptedDirectoryPath))
                return Result<bool>.Failure(["Encrypted directory does not exist"]);


            if (virtualFileSystemService.IsMounted(mountPoint))
                return Result<bool>.Failure(["Mount point is already in use"]);

            bool success = await virtualFileSystemService.MountVolumeAsync(
                encryptedDirectoryPath,
                mountPoint,
                password,
                encryptionAlgorithm,
                keyDerivationAlgorithm);

            return !success ? Result<bool>.Failure(["Failed to mount volume"]) : Result<bool>.Success(true);
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

            if (!virtualFileSystemService.IsMounted(mountPoint))
                return Result<bool>.Failure(["Mount point is not mounted"]);

            bool success = await virtualFileSystemService.UnmountVolumeAsync(mountPoint);

            return !success ? Result<bool>.Failure(["Failed to unmount volume"]) : Result<bool>.Success(true);
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
            IEnumerable<MountedVolume> mountedVolumes = virtualFileSystemService.GetMountedVolumes()
                .Select(mp => new MountedVolume
                {
                    MountPoint = mp,
                    EncryptedDirectoryPath = "Unknown", // This would need to be tracked
                    MountedAt = DateTime.UtcNow,
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

            bool isAvailable = !virtualFileSystemService.IsMounted(mountPoint);
            return Result<bool>.Success(isAvailable);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure([$"Error checking mount point availability: {ex.Message}"]);
        }
    }
}
