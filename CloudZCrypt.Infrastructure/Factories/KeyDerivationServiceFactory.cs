using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

/// <summary>
/// Provides a factory implementation that resolves a concrete <see cref="IKeyDerivationAlgorithmStrategy"/>
/// based on a specified <see cref="KeyDerivationAlgorithm"/> value.
/// </summary>
/// <remarks>
/// The factory encapsulates lookup logic over a set of registered strategy implementations supplied
/// through dependency injection. Each strategy exposes a unique <see cref="KeyDerivationAlgorithm"/> identifier
/// that is used as the key for retrieval. Attempting to request an algorithm for which no strategy was
/// registered results in an <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
/// <param name="strategies">The collection of available algorithm strategies. Must not contain duplicate identifiers.</param>
internal class KeyDerivationServiceFactory(IEnumerable<IKeyDerivationAlgorithmStrategy> strategies)
    : IKeyDerivationServiceFactory
{
    private readonly IReadOnlyDictionary<KeyDerivationAlgorithm, IKeyDerivationAlgorithmStrategy> strategies
        = strategies.ToDictionary(s => s.Id, s => s);

    /// <summary>
    /// Creates (resolves) a registered <see cref="IKeyDerivationAlgorithmStrategy"/> corresponding to the specified
    /// <paramref name="algorithm"/> value.
    /// </summary>
    /// <param name="algorithm">The key derivation algorithm to obtain a strategy for.</param>
    /// <returns>The matching <see cref="IKeyDerivationAlgorithmStrategy"/> implementation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no strategy is registered for the specified <paramref name="algorithm"/>.</exception>
    public IKeyDerivationAlgorithmStrategy Create(KeyDerivationAlgorithm algorithm)
    {
        return !strategies.TryGetValue(algorithm, out IKeyDerivationAlgorithmStrategy? strategy)
            ? throw new ArgumentOutOfRangeException(
                nameof(algorithm),
                $"Key derivation algorithm '{algorithm}' no registrado."
            )
            : strategy;
    }
}
