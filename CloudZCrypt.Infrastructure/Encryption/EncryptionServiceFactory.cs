using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace CloudZCrypt.Infrastructure.Encryption;

internal class EncryptionServiceFactory(IServiceProvider serviceProvider) : IEncryptionServiceFactory
{
    public IEncryptionService Create(EncryptionAlgorithm algorithm)
    {
        return serviceProvider.GetRequiredKeyedService<IEncryptionService>(algorithm);
    }
}
