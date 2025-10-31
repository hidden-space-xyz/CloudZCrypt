using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

/// <summary>
/// Provides an AES-256 authenticated encryption implementation using Galois/Counter Mode (GCM)
/// backed by the BouncyCastle cryptographic engine.
/// </summary>
/// <remarks>
/// This strategy derives a 256-bit key from a caller-supplied password using the selected
/// key derivation algorithm and performs streaming, chunk-based file encryption or decryption.
/// The encryption format (written to the destination file) typically includes the salt, nonce,
/// and ciphertext with the authentication tag implicitly managed by the GCM mode.
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory used to resolve a concrete key derivation algorithm strategy for password-based key derivation.</param>
internal class AesEncryptionStrategy(IKeyDerivationServiceFactory keyDerivationServiceFactory)
    : EncryptionStrategyBase(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the unique <see cref="EncryptionAlgorithm"/> identifier representing AES.
    /// </summary>
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Aes;

    /// <summary>
    /// Gets a concise, human-readable display name for the algorithm: "AES-256 GCM".
    /// </summary>
    public string DisplayName => "AES-256 GCM";

    /// <summary>
    /// Gets a detailed description of the AES-256 GCM algorithm and its characteristics.
    /// </summary>
    public string Description =>
        "A NIST-standardized 128‑bit block cipher with a 256‑bit key, widely accelerated via AES-NI and ARMv8 Cryptography Extensions. " +
        "Galois/Counter Mode (GCM) provides authenticated encryption with associated data (AEAD), combining high performance, confidentiality, and integrity.";

    /// <summary>
    /// Gets a short summary describing when this algorithm is generally preferred.
    /// </summary>
    public string Summary => "Best for general purposes (with hardware acceleration)";

    /// <summary>
    /// Performs AES-256 GCM encryption on the provided source stream and writes the ciphertext
    /// (including the authentication tag produced by GCM) to the destination stream.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing plaintext data. Must be positioned at the beginning.</param>
    /// <param name="destinationStream">Writable stream that will receive the encrypted output. Must be writable.</param>
    /// <param name="key">Symmetric encryption key (32 bytes for AES-256). Must not be null.</param>
    /// <param name="nonce">GCM nonce (recommended 12 bytes). Must be unique per key.</param>
    /// <remarks>
    /// Uses BouncyCastle's <see cref="GcmBlockCipher"/> with <see cref="AesEngine"/> and a 128-bit authentication tag.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown if an internal cipher operation fails unexpectedly.</exception>
    protected override async Task EncryptStreamAsync(
        Stream sourceStream,
        Stream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        AesEngine aesEngine = new();
        GcmBlockCipher gcmCipher = new(aesEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    /// <summary>
    /// Performs AES-256 GCM decryption on the provided source stream and writes the recovered
    /// plaintext to the destination stream after verifying the authentication tag.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing the encrypted data (ciphertext + tag). Must be positioned at the beginning.</param>
    /// <param name="destinationStream">Writable stream that will receive the decrypted plaintext. Must be writable.</param>
    /// <param name="key">Symmetric encryption key (32 bytes for AES-256) matching the one used during encryption. Must not be null.</param>
    /// <param name="nonce">Nonce used during the original encryption. Must match exactly or authentication will fail.</param>
    /// <remarks>
    /// Authentication failure (e.g., due to tampering, wrong password, or wrong nonce) results in a
    /// cryptographic exception thrown by the underlying cipher.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown when authentication fails or ciphertext is malformed.</exception>
    protected override async Task DecryptStreamAsync(
        Stream sourceStream,
        Stream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        AesEngine aesEngine = new();
        GcmBlockCipher gcmCipher = new(aesEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
