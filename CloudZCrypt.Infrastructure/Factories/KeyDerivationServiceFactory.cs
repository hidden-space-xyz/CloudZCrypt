using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

public class KeyDerivationServiceFactory(IEnumerable<IKeyDerivationAlgorithmStrategy> strategies) : IKeyDerivationServiceFactory
{
    private readonly IReadOnlyDictionary<KeyDerivationAlgorithm, IKeyDerivationAlgorithmStrategy> strategies = strategies.ToDictionary(s => s.Id, s => s);

    public IKeyDerivationAlgorithmStrategy Create(KeyDerivationAlgorithm algorithm)
    {
        if (!strategies.TryGetValue(algorithm, out IKeyDerivationAlgorithmStrategy? strategy))
            throw new ArgumentOutOfRangeException(nameof(algorithm), $"Key derivation algorithm '{algorithm}' no registrado.");
        return strategy;
    }
}
