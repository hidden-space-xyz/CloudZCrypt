using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

public interface IKeyDerivationServiceFactory
{
    IKeyDerivationAlgorithmStrategy Create(KeyDerivationAlgorithm algorithm);
}
