using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IEncryptionService
{
    Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
    Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
}
