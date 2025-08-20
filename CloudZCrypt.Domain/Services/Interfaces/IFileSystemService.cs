using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IFileSystemService
{
    Task<bool> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm);
    Task<bool> UnmountVolumeAsync(string mountPoint);
    IEnumerable<string> GetMountedVolumes();
    bool IsMounted(string mountPoint);
    IEnumerable<MountPoint> GetAvailableMountPoints();
}
