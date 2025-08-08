using CloudZCrypt.Application.Constants;

namespace CloudZCrypt.Application.Interfaces.Encryption
{
    public interface IEncryptionServiceFactory
    {
        IEncryptionService Create(EncryptionAlgorithm algorithm);
    }
}
