using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

/// <summary>
/// Factory interface for creating name obfuscation strategy instances based on the specified algorithm.
/// </summary>
/// <remarks>
/// This factory follows the same pattern as other strategy factories in the domain, providing a clean
/// way to resolve concrete strategy implementations from enumeration values.
/// </remarks>
public interface INameObfuscationServiceFactory
{
    /// <summary>
    /// Creates (resolves) a registered <see cref="INameObfuscationStrategy"/> corresponding to the specified
    /// <paramref name="obfuscationMode"/> value.
    /// </summary>
    /// <param name="obfuscationMode">The name obfuscation mode to obtain a strategy for.</param>
    /// <returns>The matching <see cref="INameObfuscationStrategy"/> implementation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no strategy is registered for the specified <paramref name="obfuscationMode"/>.</exception>
    INameObfuscationStrategy Create(NameObfuscationMode obfuscationMode);
}