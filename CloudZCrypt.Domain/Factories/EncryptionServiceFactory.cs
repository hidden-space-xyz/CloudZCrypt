using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CloudZCrypt.Domain.Factories;

internal class EncryptionServiceFactory(IServiceProvider serviceProvider) : IEncryptionServiceFactory
{
    public IEncryptionService Create(EncryptionAlgorithm algorithm)
    {
        return serviceProvider.GetRequiredKeyedService<IEncryptionService>(algorithm);
    }
}
