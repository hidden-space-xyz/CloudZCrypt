using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IFileSystemApplicationService
{
    /// <summary>
    /// Mounts an encrypted directory as a virtual volume
    /// </summary>
    Task<Result<bool>> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm);

    /// <summary>
    /// Unmounts a virtual volume
    /// </summary>
    Task<Result<bool>> UnmountVolumeAsync(string mountPoint);

    /// <summary>
    /// Gets all mounted volumes
    /// </summary>
    Task<Result<IEnumerable<MountedVolume>>> GetMountedVolumesAsync();

    /// <summary>
    /// Checks if a mount point is available
    /// </summary>
    Task<Result<bool>> IsMountPointAvailableAsync(string mountPoint);
}