using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Strategies.Interfaces;

/// <summary>
/// Strategy interface that defines metadata and obfuscation logic for filename obfuscation algorithms.
/// </summary>
/// <remarks>
/// This interface follows the same design pattern as <see cref="IEncryptionAlgorithmStrategy"/> and 
/// <see cref="IKeyDerivationAlgorithmStrategy"/>, providing consistent metadata exposure for UI binding
/// and encapsulating the specific logic for each obfuscation mode.
/// </remarks>
public interface INameObfuscationStrategy
{
    /// <summary>
    /// Gets the unique identifier of the name obfuscation mode represented by this strategy.
    /// </summary>
    /// <remarks>
    /// The value corresponds to a member of the <see cref="NameObfuscationMode"/> enumeration and
    /// is used for selection, factory resolution, and persistence of user preferences.
    /// </remarks>
    NameObfuscationMode Id { get; }

    /// <summary>
    /// Gets a short, human-readable name suitable for user interface display (e.g., a dropdown).
    /// </summary>
    /// <remarks>
    /// This value should be localized where appropriate and should not be relied upon for programmatic logic.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets a concise description explaining the obfuscation method's characteristics and typical usage scenarios.
    /// </summary>
    /// <remarks>
    /// Intended for tooltips or help panels to assist users in selecting an appropriate obfuscation method.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets a brief summary highlighting the obfuscation method's primary strengths or characteristics.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Description"/>, this value should be a very short phrase suitable for compact UI contexts.
    /// </remarks>
    string Summary { get; }

    /// <summary>
    /// Generates an obfuscated filename based on the strategy's algorithm and the provided source file information.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file being processed.</param>
    /// <param name="originalFileName">The original filename (with or without extension).</param>
    /// <returns>The obfuscated filename with the same extension as the original.</returns>
    /// <remarks>
    /// The returned filename should preserve the original extension to maintain file type associations.
    /// For strategies that don't obfuscate (None), the original filename should be returned unchanged.
    /// </remarks>
    string ObfuscateFileName(string sourceFilePath, string originalFileName);
}