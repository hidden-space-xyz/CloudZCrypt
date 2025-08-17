using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IFileSystemApplicationService
{
    Task<Result<bool>> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm);
    Task<Result<bool>> UnmountVolumeAsync(string mountPoint);
    Task<Result<IEnumerable<MountedVolume>>> GetMountedVolumesAsync();
    Task<Result<bool>> IsMountPointAvailableAsync(string mountPoint);
}
