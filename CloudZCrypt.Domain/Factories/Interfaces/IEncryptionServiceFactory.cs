using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

public interface IEncryptionServiceFactory
{
    IEncryptionService Create(EncryptionAlgorithm algorithm);
}
