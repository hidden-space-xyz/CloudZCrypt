using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Strategy interface that exposes metadata for a key derivation algorithm.
/// Extends <see cref="IKeyDerivationService"/> so existing derivation logic is reused.
/// </summary>
public interface IKeyDerivationAlgorithmStrategy
{
    /// <summary>
    /// Gets the unique identifier of the key derivation algorithm represented by this strategy.
    /// </summary>
    /// <remarks>
    /// The value corresponds to a member of the <see cref="KeyDerivationAlgorithm"/> enumeration and
    /// is used for selection, factory resolution, and persistence of user preferences.
    /// </remarks>
    KeyDerivationAlgorithm Id { get; }

    /// <summary>
    /// Gets a short, human-readable name suitable for user interface display (e.g., a dropdown).
    /// </summary>
    /// <remarks>
    /// This value should be localized where appropriate and should not be relied upon for programmatic logic.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets a concise description explaining the algorithm's characteristics and typical usage scenarios.
    /// </summary>
    /// <remarks>
    /// Intended for tooltips or help panels to assist users in selecting an appropriate algorithm.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets a brief summary highlighting the algorithm's primary strengths (e.g., performance, memory hardness).
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Description"/>, this value should be a very short phrase suitable for compact UI contexts.
    /// </remarks>
    string Summary { get; }

    /// <summary>
    /// Derives a cryptographic key from the supplied password and salt using the algorithm parameters embodied by this strategy.
    /// </summary>
    /// <param name="password">The user-supplied secret (passphrase). Must not be null or empty.</param>
    /// <param name="salt">A cryptographically strong, unique salt. Must not be null and should be at least 16 bytes.</param>
    /// <param name="keySize">The desired length of the derived key in bytes. Must be a positive integer appropriate for the target cipher.</param>
    /// <returns>A byte array containing the derived key of the requested length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> or <paramref name="salt"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is empty, <paramref name="salt"/> is empty, or <paramref name="keySize"/> is not positive.</exception>
    byte[] DeriveKey(string password, byte[] salt, int keySize);
}
