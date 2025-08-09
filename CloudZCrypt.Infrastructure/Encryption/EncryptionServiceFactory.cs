using CloudZCrypt.Application.Interfaces.Encryption;
using CloudZCrypt.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace CloudZCrypt.Infrastructure.Encryption;

internal class EncryptionServiceFactory(IServiceProvider serviceProvider) : IEncryptionServiceFactory
{
    public IEncryptionService Create(EncryptionAlgorithm algorithm)
    {
        return serviceProvider.GetRequiredKeyedService<IEncryptionService>(algorithm);
    }
}
