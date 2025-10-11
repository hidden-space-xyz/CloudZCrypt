using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Factories;

/// <summary>
/// Provides a factory implementation that resolves a concrete <see cref="INameObfuscationStrategy"/>
/// based on a specified <see cref="NameObfuscationMode"/> value.
/// </summary>
/// <remarks>
/// The factory encapsulates lookup logic over a set of registered strategy implementations supplied
/// through dependency injection. Each strategy exposes a unique <see cref="NameObfuscationMode"/> identifier
/// that is used as the key for retrieval. Attempting to request a mode for which no strategy was
/// registered results in an <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
/// <param name="strategies">The collection of available obfuscation strategies. Must not contain duplicate identifiers.</param>
internal class NameObfuscationServiceFactory(IEnumerable<INameObfuscationStrategy> strategies)
    : INameObfuscationServiceFactory
{
    private readonly IReadOnlyDictionary<NameObfuscationMode, INameObfuscationStrategy> strategies
        = strategies.ToDictionary(s => s.Id, s => s);

    /// <summary>
    /// Creates (resolves) a registered <see cref="INameObfuscationStrategy"/> corresponding to the specified
    /// <paramref name="obfuscationMode"/> value.
    /// </summary>
    /// <param name="obfuscationMode">The name obfuscation mode to obtain a strategy for.</param>
    /// <returns>The matching <see cref="INameObfuscationStrategy"/> implementation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no strategy is registered for the specified <paramref name="obfuscationMode"/>.</exception>
    public INameObfuscationStrategy Create(NameObfuscationMode obfuscationMode)
    {
        return !strategies.TryGetValue(obfuscationMode, out INameObfuscationStrategy? strategy)
            ? throw new ArgumentOutOfRangeException(nameof(obfuscationMode), $"Name obfuscation mode '{obfuscationMode}' is not registered.")
            : strategy;
    }
}