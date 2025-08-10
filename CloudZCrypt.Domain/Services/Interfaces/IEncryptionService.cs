namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IEncryptionService
{
    public Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password);
    public Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password);
}