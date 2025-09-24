using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

/// <summary>
/// Defines a factory responsible for producing concrete <see cref="IKeyDerivationAlgorithmStrategy"/> instances
/// based on a specified <see cref="KeyDerivationAlgorithm"/> value.
/// </summary>
/// <remarks>
/// This abstraction decouples algorithm selection logic from consumers that require password-based key
/// derivation (key stretching) functionality. It enables easy extension with additional algorithms while
/// centralizing validation and instantiation concerns in implementing types.
/// Implementations should be thread-safe if they are registered as singletons in a dependency injection container.
/// </remarks>
public interface IKeyDerivationServiceFactory
{
    /// <summary>
    /// Creates a concrete <see cref="IKeyDerivationAlgorithmStrategy"/> corresponding to the specified
    /// <paramref name="algorithm"/> identifier.
    /// </summary>
    /// <param name="algorithm">The key derivation algorithm to instantiate a strategy for.</param>
    /// <returns>An <see cref="IKeyDerivationAlgorithmStrategy"/> implementation suited to the requested algorithm.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="algorithm"/> is not a defined enum value.</exception>
    /// <exception cref="System.NotSupportedException">Thrown when the algorithm is recognized but no implementation is available.</exception>
    IKeyDerivationAlgorithmStrategy Create(KeyDerivationAlgorithm algorithm);
}
