using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

/// <summary>
/// Provides a factory for resolving concrete <see cref="IEncryptionAlgorithmStrategy"/> implementations
/// based on a specified <see cref="EncryptionAlgorithm"/> value.
/// </summary>
/// <remarks>
/// This factory leverages dependency injection to receive all available algorithm strategy implementations
/// and exposes a single method for retrieving the strategy that matches a requested algorithm. If a strategy
/// has not been registered for the provided algorithm, an <see cref="ArgumentOutOfRangeException"/> is thrown.
/// </remarks>
internal class EncryptionServiceFactory(IEnumerable<IEncryptionAlgorithmStrategy> strategies)
    : IEncryptionServiceFactory
{
    /// <summary>
    /// Mapping of supported <see cref="EncryptionAlgorithm"/> values to their corresponding
    /// <see cref="IEncryptionAlgorithmStrategy"/> implementations. Populated from the injected strategies collection.
    /// </summary>
    private readonly IReadOnlyDictionary<EncryptionAlgorithm, IEncryptionAlgorithmStrategy> strategies
        = strategies.ToDictionary(s => s.Id, s => s);

    /// <summary>
    /// Resolves and returns the registered <see cref="IEncryptionAlgorithmStrategy"/> for the specified
    /// <paramref name="algorithm"/>.
    /// </summary>
    /// <param name="algorithm">The <see cref="EncryptionAlgorithm"/> identifying the desired encryption strategy.</param>
    /// <returns>The concrete <see cref="IEncryptionAlgorithmStrategy"/> matching the requested algorithm.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no strategy is registered for the specified <paramref name="algorithm"/>.</exception>
    public IEncryptionAlgorithmStrategy Create(EncryptionAlgorithm algorithm)
    {
        return !strategies.TryGetValue(algorithm, out IEncryptionAlgorithmStrategy? strategy)
            ? throw new ArgumentOutOfRangeException(
                nameof(algorithm),
                $"Encryption algorithm '{algorithm}' no registrado."
            )
            : strategy;
    }
}
