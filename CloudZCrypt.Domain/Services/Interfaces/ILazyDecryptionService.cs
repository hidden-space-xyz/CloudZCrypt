using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Domain.Services.Interfaces;

public interface ILazyDecryptionService
{
    /// <summary>
    /// Creates directory structure without decrypting files during mount
    /// </summary>
    Task CreateVaultStructureAsync(
        string encryptedDirectoryPath,
        string mountDirectoryPath,
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm);

    /// <summary>
    /// Decrypts a specific file on-demand when accessed
    /// </summary>
    Task<bool> DecryptFileOnDemandAsync(
        string encryptedFilePath,
        string decryptedFilePath,
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm);

    /// <summary>
    /// Checks if a file needs to be decrypted (exists as encrypted but not as decrypted)
    /// </summary>
    Task<bool> RequiresDecryptionAsync(
        string encryptedFilePath,
        string decryptedFilePath);

    /// <summary>
    /// Gets the encrypted file path for a given decrypted path
    /// </summary>
    string GetEncryptedFilePath(
        string decryptedFilePath,
        string decryptedRoot,
        string encryptedRoot);
}