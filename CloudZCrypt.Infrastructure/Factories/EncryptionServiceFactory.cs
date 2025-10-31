using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

internal class EncryptionServiceFactory(IEnumerable<IEncryptionAlgorithmStrategy> strategies)
    : IEncryptionServiceFactory
{
    private readonly IReadOnlyDictionary<EncryptionAlgorithm, IEncryptionAlgorithmStrategy> strategies
        = strategies.ToDictionary(s => s.Id, s => s);

    public IEncryptionAlgorithmStrategy Create(EncryptionAlgorithm algorithm)
    {
        return !strategies.TryGetValue(algorithm, out IEncryptionAlgorithmStrategy? strategy)
            ? throw new ArgumentOutOfRangeException(nameof(algorithm), $"Encryption algorithm '{algorithm}' no registrado.")
            : strategy;
    }
}
