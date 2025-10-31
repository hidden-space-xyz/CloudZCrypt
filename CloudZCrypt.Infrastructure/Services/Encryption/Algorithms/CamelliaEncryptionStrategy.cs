using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

/// <summary>
/// Provides a Camellia-256 authenticated encryption implementation using Galois/Counter Mode (GCM)
/// via the BouncyCastle Camellia block cipher engine.
/// </summary>
/// <remarks>
/// Camellia is a standardized 128-bit block cipher (ISO/IEC, RFCs) with performance and security
/// properties broadly comparable to AES. This service derives a 256-bit key from a password using
/// a configurable key derivation strategy and applies Camellia within GCM to deliver confidentiality,
/// integrity, and authenticity.
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory used to resolve password-based key derivation strategies.</param>
internal class CamelliaEncryptionStrategy(IKeyDerivationServiceFactory keyDerivationServiceFactory)
    : EncryptionStrategyBase(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the <see cref="EncryptionAlgorithm"/> identifier representing Camellia.
    /// </summary>
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Camellia;

    /// <summary>
    /// Gets the display name: "Camellia-256 GCM".
    /// </summary>
    public string DisplayName => "Camellia-256 GCM";

    /// <summary>
    /// Gets a descriptive overview of the Camellia cipher and its intended usage contexts.
    /// </summary>
    public string Description =>
        "A 128‑bit block cipher with a 256‑bit key, jointly designed by NTT and Mitsubishi; performance and security margin comparable to AES. " +
        "Supported in many international standards (RFCs, ISO/IEC) and suitable where non‑U.S.-origin algorithms or broader jurisdictional acceptance is desired. " +
        "Used with GCM for AEAD.";

    /// <summary>
    /// Gets a concise summary indicating when Camellia may be preferred.
    /// </summary>
    public string Summary => "Best for international compliance";

    /// <summary>
    /// Encrypts plaintext from <paramref name="sourceStream"/> using Camellia-GCM and writes the
    /// resulting ciphertext plus authentication tag to <paramref name="destinationStream"/>.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing plaintext input. Must be positioned at start.</param>
    /// <param name="destinationStream">Writable stream receiving ciphertext output. Must be writable.</param>
    /// <param name="key">Derived 256-bit symmetric key. Must not be null.</param>
    /// <param name="nonce">GCM nonce (typically 12 bytes). Must be unique per key.</param>
    /// <remarks>
    /// Uses <see cref="GcmBlockCipher"/> with <see cref="CamelliaEngine"/> producing a 128-bit authentication tag.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown if encryption fails during processing or finalization.</exception>
    protected override async Task EncryptStreamAsync(
        Stream sourceStream,
        Stream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    /// <summary>
    /// Decrypts Camellia-GCM ciphertext from <paramref name="sourceStream"/> and writes the recovered
    /// plaintext to <paramref name="destinationStream"/> after verifying authenticity.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing ciphertext + tag. Must be at the beginning.</param>
    /// <param name="destinationStream">Writable stream for plaintext output. Must be writable.</param>
    /// <param name="key">The same 256-bit key used during encryption. Must not be null.</param>
    /// <param name="nonce">Nonce used during encryption. Must match exactly to authenticate correctly.</param>
    /// <remarks>
    /// Authentication failure (tampering, incorrect password, wrong nonce) produces a cryptographic exception.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown when authentication fails or ciphertext is malformed.</exception>
    protected override async Task DecryptStreamAsync(
        Stream sourceStream,
        Stream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
