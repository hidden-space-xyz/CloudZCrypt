using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

/// <summary>
/// Provides a Twofish-256 authenticated encryption implementation using Galois/Counter Mode (GCM)
/// over the Twofish block cipher via the BouncyCastle library.
/// </summary>
/// <remarks>
/// Twofish (an AES finalist) offers algorithmic diversity through its Feistel structure and
/// key-dependent S-box design philosophy. This service derives a 256-bit key from a password
/// (through a selected key derivation strategy) and applies AEAD semantics using GCM to supply
/// confidentiality, integrity, and authenticity.
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory resolving concrete key derivation strategies for password-based key derivation.</param>
internal class TwofishEncryptionStrategy(IKeyDerivationServiceFactory keyDerivationServiceFactory)
    : EncryptionStrategyBase(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the unique <see cref="EncryptionAlgorithm"/> identifier representing Twofish.
    /// </summary>
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Twofish;

    /// <summary>
    /// Gets the human-readable display name for the algorithm: "Twofish-256 GCM".
    /// </summary>
    public string DisplayName => "Twofish-256 GCM";

    /// <summary>
    /// Gets a descriptive text highlighting Twofish design characteristics and its positioning.
    /// </summary>
    public string Description =>
        "A flexible 128‑bit block cipher (up to 256‑bit keys), also an AES finalist. " +
        "Offers solid cryptanalytic resilience with a different design philosophy (Feistel + key-dependent S‑boxes) for algorithmic diversity. " +
        "Less commonly hardware-accelerated or standardized for AEAD modes.";

    /// <summary>
    /// Gets a concise summary describing when this algorithm may be preferred.
    /// </summary>
    public string Summary => "Best for design diversity";

    /// <summary>
    /// Encrypts the plaintext from <paramref name="sourceStream"/> using Twofish in GCM mode and
    /// writes the resulting ciphertext (with authentication tag) to <paramref name="destinationStream"/>.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing the plaintext input. Must be at the beginning.</param>
    /// <param name="destinationStream">Writable stream receiving the encrypted output. Must support writing.</param>
    /// <param name="key">Derived symmetric key (32 bytes for 256-bit strength). Must not be null.</param>
    /// <param name="nonce">GCM nonce (ideally 12 bytes). Must be unique per key.</param>
    /// <remarks>
    /// Utilizes <see cref="GcmBlockCipher"/> with <see cref="TwofishEngine"/> producing a 128-bit authentication tag.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Raised if an internal encryption or finalization step fails unexpectedly.</exception>
    protected override async Task EncryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    /// <summary>
    /// Decrypts Twofish-GCM ciphertext from <paramref name="sourceStream"/> and writes the recovered
    /// plaintext to <paramref name="destinationStream"/> after authentication verification.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing ciphertext + tag. Must be at the beginning.</param>
    /// <param name="destinationStream">Writable stream receiving the decrypted plaintext. Must support writing.</param>
    /// <param name="key">The same symmetric key (32 bytes) used during encryption. Must not be null.</param>
    /// <param name="nonce">Nonce supplied during encryption. Must match exactly for successful authentication.</param>
    /// <remarks>
    /// Authentication failure (tampering, wrong password/nonce) results in a cryptographic exception from the cipher.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown when authentication fails or ciphertext is malformed.</exception>
    protected override async Task DecryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
