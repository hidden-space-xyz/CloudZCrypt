using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Defines a strategy contract for performing symmetric file encryption and decryption
/// operations using a specific <see cref="EncryptionAlgorithm"/> implementation.
/// </summary>
/// <remarks>
/// Implementations encapsulate the algorithm-specific details such as cipher mode, padding,
/// key size selection, nonces/IV handling, authentication tags (if applicable), and secure
/// resource disposal. This interface is consumed by higher-level orchestrators or factories
/// that resolve an algorithm at runtime based on user input or configuration.
/// <para>
/// Implementations should be:
/// <list type="bullet">
/// <item><description>Stateless or safely reusable across calls (thread-safe where possible).</description></item>
/// <item><description>Explicit about any constraints (e.g., maximum file size, required entropy, platform dependencies).</description></item>
/// <item><description>Resilient to partial failures (e.g., ensure temporary artifacts are cleaned up on exceptions).</description></item>
/// </list>
/// </para>
/// <para>
/// Example (conceptual usage):
/// <code language="csharp">
/// IEncryptionAlgorithmStrategy strategy = factory.Create(EncryptionAlgorithm.Aes);
/// bool success = await strategy.EncryptFileAsync(
///     sourceFilePath: inputPath,
///     destinationFilePath: outputPath,
///     password: userPassword,
///     keyDerivationAlgorithm: KeyDerivationAlgorithm.Argon2id);
/// </code>
/// </para>
/// </remarks>
public interface IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the unique algorithm identifier corresponding to the underlying cryptographic primitive.
    /// </summary>
    /// <remarks>
    /// This value is typically used for selection, logging, auditing, and serialization scenarios.
    /// </remarks>
    EncryptionAlgorithm Id { get; }

    /// <summary>
    /// Gets a short, human-readable name for the encryption algorithm (e.g., "AES-256").
    /// </summary>
    /// <remarks>
    /// Intended for display in UI elements, logs, or selection menus.
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets a descriptive text providing additional context about the algorithm's characteristics.
    /// </summary>
    /// <remarks>
    /// This may include performance considerations, security posture, and applicable usage scenarios.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets a concise summary suitable for tooltip or compact display contexts.
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Encrypts the specified source file using the algorithm strategy and writes the ciphertext
    /// (and any required metadata) to the destination path.
    /// </summary>
    /// <param name="sourceFilePath">The absolute or relative path to the plaintext input file. Must exist and be readable.</param>
    /// <param name="destinationFilePath">The target path where the encrypted file (or container) will be written. Will be overwritten if it exists.</param>
    /// <param name="password">The user-supplied secret used to derive the encryption key. Must not be null or empty.</param>
    /// <param name="keyDerivationAlgorithm">The password-based key derivation function to apply when deriving the cryptographic key material.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is <c>true</c> if the encryption succeeded; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceFilePath"/>, <paramref name="destinationFilePath"/>, or <paramref name="password"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if paths are empty, whitespace, or invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the source file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if read/write permissions are insufficient for the provided paths.</exception>
    /// <exception cref="CryptographicException">Thrown if a cryptographic operation fails (e.g., key derivation or cipher initialization).</exception>
    Task<bool> EncryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string password,
        KeyDerivationAlgorithm keyDerivationAlgorithm
    );

    /// <summary>
    /// Decrypts the specified encrypted source file and writes the recovered plaintext to the destination path.
    /// </summary>
    /// <param name="sourceFilePath">The absolute or relative path to the encrypted input file. Must exist and be readable.</param>
    /// <param name="destinationFilePath">The target path where the decrypted plaintext file will be written. Will be overwritten if it exists.</param>
    /// <param name="password">The user-supplied secret used to derive or validate the decryption key. Must match the original encryption password.</param>
    /// <param name="keyDerivationAlgorithm">The password-based key derivation function expected for this encrypted artifact.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is <c>true</c> if the decryption succeeded and integrity/authenticity checks (if any) passed; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceFilePath"/>, <paramref name="destinationFilePath"/>, or <paramref name="password"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if paths are empty, whitespace, or invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the source (encrypted) file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if read/write permissions are insufficient for the provided paths.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails due to corrupted data, integrity/authentication failure, or incorrect password.</exception>
    Task<bool> DecryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string password,
        KeyDerivationAlgorithm keyDerivationAlgorithm
    );
}
