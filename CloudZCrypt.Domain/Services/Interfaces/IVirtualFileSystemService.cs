using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IVirtualFileSystemService
{
    /// <summary>
    /// Mounts an encrypted directory as a virtual drive
    /// </summary>
    Task<bool> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm);

    /// <summary>
    /// Unmounts a virtual drive
    /// </summary>
    Task<bool> UnmountVolumeAsync(string mountPoint);

    /// <summary>
    /// Gets all currently mounted volumes
    /// </summary>
    IEnumerable<string> GetMountedVolumes();

    /// <summary>
    /// Checks if a mount point is currently mounted
    /// </summary>
    bool IsMounted(string mountPoint);
}