using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Service responsible for on-demand file decryption when files are accessed,
/// similar to VeraCrypt's behavior.
/// </summary>
public interface IOnDemandDecryptionService
{
    /// <summary>
    /// Initializes the on-demand decryption for a mounted volume
    /// </summary>
    /// <param name="encryptedDirectoryPath">Path to the encrypted vault directory</param>
    /// <param name="tempDirectoryPath">Path to the temporary mount directory</param>
    /// <param name="password">Decryption password</param>
    /// <param name="encryptionAlgorithm">Encryption algorithm used</param>
    /// <param name="keyDerivationAlgorithm">Key derivation algorithm used</param>
    /// <returns>True if initialization succeeded</returns>
    Task<bool> InitializeAsync(
        string encryptedDirectoryPath,
        string tempDirectoryPath,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm);

    /// <summary>
    /// Creates the virtual directory structure with placeholder files
    /// </summary>
    /// <param name="encryptedDirectoryPath">Path to the encrypted vault</param>
    /// <param name="virtualDirectoryPath">Path where virtual structure should be created</param>
    /// <returns>True if structure creation succeeded</returns>
    Task<bool> CreateVirtualDirectoryStructureAsync(string encryptedDirectoryPath, string virtualDirectoryPath);

    /// <summary>
    /// Decrypts a file on-demand when it's accessed
    /// </summary>
    /// <param name="virtualFilePath">Path to the virtual/placeholder file</param>
    /// <returns>True if decryption succeeded</returns>
    Task<bool> DecryptFileOnDemandAsync(string virtualFilePath);

    /// <summary>
    /// Checks if a file needs to be decrypted (is currently a placeholder)
    /// </summary>
    /// <param name="filePath">Path to check</param>
    /// <returns>True if file needs decryption</returns>
    bool IsPlaceholderFile(string filePath);

    /// <summary>
    /// Cleans up cached decrypted files that haven't been accessed recently
    /// </summary>
    Task CleanupUnusedFilesAsync();

    /// <summary>
    /// Cleans up all resources and temporary files
    /// </summary>
    void Dispose();
}