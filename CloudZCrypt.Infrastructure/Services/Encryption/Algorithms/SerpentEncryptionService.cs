using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

/// <summary>
/// Provides a Serpent-256 authenticated encryption implementation using Galois/Counter Mode (GCM)
/// atop the Serpent block cipher through the BouncyCastle cryptographic primitives.
/// </summary>
/// <remarks>
/// Serpent, another AES competition finalist, emphasizes a large security margin via a conservative
/// design and 32-round structure. While typically slower than AES or Camellia, it may be selected
/// for defense-in-depth or diversity objectives. This service derives a 256-bit key from a password
/// using a pluggable key derivation strategy, then applies Serpent within GCM for AEAD guarantees.
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory responsible for resolving key derivation strategies.</param>
public class SerpentEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory)
    : BaseEncryptionService(keyDerivationServiceFactory),
        IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the <see cref="EncryptionAlgorithm"/> identifier representing Serpent.
    /// </summary>
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Serpent;

    /// <summary>
    /// Gets the human-readable display name: "Serpent-256 GCM".
    /// </summary>
    public string DisplayName => "Serpent-256 GCM";

    /// <summary>
    /// Gets a detailed description of Serpent's characteristics and usage context.
    /// </summary>
    public string Description =>
        "A conservative 128‑bit block cipher finalist from the AES competition, designed with a large security margin and a 256‑bit key option. Typically slower than AES and Camellia. When wrapped in GCM it provides AEAD, but performance costs make it niche for high-assurance or defense-in-depth scenarios.";

    /// <summary>
    /// Gets a concise summary describing when Serpent may be preferred.
    /// </summary>
    public string Summary => "Best for high-security purposes (slow)";

    /// <summary>
    /// Encrypts plaintext from <paramref name="sourceStream"/> using Serpent in GCM mode and writes
    /// the ciphertext plus authentication tag to <paramref name="destinationStream"/>.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing plaintext input. Should be positioned at start.</param>
    /// <param name="destinationStream">Writable stream receiving the encrypted output. Must be writable.</param>
    /// <param name="key">Derived symmetric key (32 bytes). Must not be null.</param>
    /// <param name="nonce">GCM nonce (commonly 12 bytes). Must be unique per key.</param>
    /// <remarks>
    /// Uses <see cref="GcmBlockCipher"/> with <see cref="SerpentEngine"/> producing a 128-bit authentication tag.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown if encryption fails during finalization.</exception>
    protected override async Task EncryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        SerpentEngine serpentEngine = new();
        GcmBlockCipher gcmCipher = new(serpentEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    /// <summary>
    /// Decrypts Serpent-GCM ciphertext from <paramref name="sourceStream"/> and writes the recovered
    /// plaintext to <paramref name="destinationStream"/> after verifying integrity and authenticity.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing ciphertext and tag. Should be at start.</param>
    /// <param name="destinationStream">Writable stream for the resulting plaintext. Must be writable.</param>
    /// <param name="key">The original 256-bit key used for encryption. Must not be null.</param>
    /// <param name="nonce">The nonce supplied during encryption. Must match exactly.</param>
    /// <remarks>
    /// A failed authentication check (corruption, tampering, wrong password or nonce) results in a cryptographic exception.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown when authentication fails or data is malformed.</exception>
    protected override async Task DecryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        SerpentEngine serpentEngine = new();
        GcmBlockCipher gcmCipher = new(serpentEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
