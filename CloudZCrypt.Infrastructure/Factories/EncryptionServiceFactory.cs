using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

public class EncryptionServiceFactory(IEnumerable<IEncryptionAlgorithmStrategy> strategies) : IEncryptionServiceFactory
{
    private readonly IReadOnlyDictionary<EncryptionAlgorithm, IEncryptionAlgorithmStrategy> _strategies = strategies.ToDictionary(s => s.Id, s => s);

    public IEncryptionAlgorithmStrategy Create(EncryptionAlgorithm algorithm)
    {
        if (!_strategies.TryGetValue(algorithm, out IEncryptionAlgorithmStrategy? strategy))
            throw new ArgumentOutOfRangeException(nameof(algorithm), $"Encryption algorithm '{algorithm}' no registrado.");

        return strategy;
    }
}
