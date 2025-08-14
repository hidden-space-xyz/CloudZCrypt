using CloudZCrypt.Domain.Constants;

namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IEncryptionService
{
    public Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
    public Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm);
}