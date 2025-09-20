using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CloudZCrypt.Infrastructure.Factories;

public class KeyDerivationServiceFactory(IServiceProvider serviceProvider) : IKeyDerivationServiceFactory
{
    public IKeyDerivationService Create(KeyDerivationAlgorithm algorithm)
    {
        return serviceProvider.GetRequiredKeyedService<IKeyDerivationService>(algorithm);
    }
}
