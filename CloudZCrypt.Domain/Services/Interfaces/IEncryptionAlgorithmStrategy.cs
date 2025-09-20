using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Interface for encryption algorithm strategies.
/// This interface defines the contract for encryption and decryption operations,
/// as well as properties to describe the algorithm.
/// </summary>
public interface IEncryptionAlgorithmStrategy
{
    EncryptionAlgorithm Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Summary { get; }

    Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
    Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
}
