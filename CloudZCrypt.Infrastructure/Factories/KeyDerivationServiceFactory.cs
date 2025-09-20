using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

public class KeyDerivationServiceFactory(IEnumerable<IKeyDerivationAlgorithmStrategy> strategies) : IKeyDerivationServiceFactory
{
    private readonly IReadOnlyDictionary<KeyDerivationAlgorithm, IKeyDerivationAlgorithmStrategy> _strategies = strategies.ToDictionary(s => s.Id, s => s);

    public IKeyDerivationService Create(KeyDerivationAlgorithm algorithm)
    {
        if (!_strategies.TryGetValue(algorithm, out IKeyDerivationAlgorithmStrategy? strategy))
            throw new ArgumentOutOfRangeException(nameof(algorithm), $"Key derivation algorithm '{algorithm}' no registrado.");
        return strategy; // Estrategia implementa IKeyDerivationService
    }
}
