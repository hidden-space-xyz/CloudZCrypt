using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

public interface IEncryptionServiceFactory
{
    IEncryptionService Create(EncryptionAlgorithm algorithm);
}
