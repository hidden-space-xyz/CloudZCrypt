using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

/// <summary>
/// Implements the ChaCha20-Poly1305 AEAD construction (RFC 8439) for streaming file encryption
/// and decryption using the BouncyCastle cryptographic library.
/// </summary>
/// <remarks>
/// ChaCha20 provides high-performance, constant-time symmetric encryption on a wide range of CPUs,
/// especially where AES hardware acceleration (AES-NI / ARMv8 crypto extensions) is absent or limited.
/// Combined with the Poly1305 authenticator, this service supplies confidentiality, integrity, and
/// authenticity. A 256-bit key is derived from a password using a pluggable key derivation strategy
/// (e.g., Argon2id, PBKDF2). The nonce must be unique per key; reuse compromises security.
/// <para>
/// Example usage (conceptual):
/// <code language="csharp">
/// IEncryptionAlgorithmStrategy strategy = new ChaCha20EncryptionService(keyDerivationFactory);
/// bool ok = await strategy.EncryptFileAsync(
///     sourceFilePath: "plain.bin",
///     destinationFilePath: "secret.chacha20",
///     password: userPassword,
///     keyDerivationAlgorithm: KeyDerivationAlgorithm.Argon2id);
/// </code>
/// </para>
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory resolving password-based key derivation strategies.</param>
public class ChaCha20EncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory)
    : BaseEncryptionService(keyDerivationServiceFactory),
        IEncryptionAlgorithmStrategy
{
    /// <summary>
    /// Gets the <see cref="EncryptionAlgorithm"/> identifier for ChaCha20-Poly1305.
    /// </summary>
    public EncryptionAlgorithm Id => EncryptionAlgorithm.ChaCha20;

    /// <summary>
    /// Gets the human-readable display name: "ChaCha20-Poly1305".
    /// </summary>
    public string DisplayName => "ChaCha20-Poly1305";

    /// <summary>
    /// Gets a descriptive overview of ChaCha20-Poly1305 and typical deployment scenarios.
    /// </summary>
    public string Description =>
        "A modern ARX (Add-Rotate-XOR) stream cipher (ChaCha20) combined with the Poly1305 MAC to form a fast, timing‑attack‑resistant AEAD construction that performs especially well on devices lacking AES hardware acceleration. Standardized in RFC 8439 and widely deployed in TLS, SSH, QUIC, and WireGuard.";

    /// <summary>
    /// Gets a concise summary describing when ChaCha20-Poly1305 is preferred.
    /// </summary>
    public string Summary => "Best for general purposes (without hardware acceleration)";

    /// <summary>
    /// Encrypts plaintext from <paramref name="sourceStream"/> using ChaCha20-Poly1305 and writes the
    /// ciphertext plus authentication tag to <paramref name="destinationStream"/>.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing plaintext input. Must be positioned at start.</param>
    /// <param name="destinationStream">Writable stream receiving ciphertext output. Must be writable.</param>
    /// <param name="key">Derived 256-bit symmetric key. Must not be null.</param>
    /// <param name="nonce">AEAD nonce (typically 12 bytes). Must be unique per key.</param>
    /// <remarks>
    /// Utilizes <see cref="ChaCha20Poly1305"/> via an AEAD interface with a 128-bit authentication tag.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown if encryption fails during finalization.</exception>
    protected override async Task EncryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        ChaCha20Poly1305 chacha20Poly1305 = new();
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        chacha20Poly1305.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, chacha20Poly1305);
    }

    /// <summary>
    /// Decrypts ChaCha20-Poly1305 ciphertext from <paramref name="sourceStream"/> and writes the
    /// recovered plaintext to <paramref name="destinationStream"/> after authentication verification.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing ciphertext + tag. Must be positioned at start.</param>
    /// <param name="destinationStream">Writable stream receiving plaintext output. Must be writable.</param>
    /// <param name="key">The original 256-bit key used for encryption. Must not be null.</param>
    /// <param name="nonce">Nonce used during encryption. Must match exactly or authentication fails.</param>
    /// <remarks>
    /// Authentication failure (tampering, wrong key/password, or nonce mismatch) leads to a cryptographic exception.
    /// </remarks>
    /// <exception cref="Org.BouncyCastle.Crypto.InvalidCipherTextException">Thrown when authentication fails or ciphertext is malformed.</exception>
    protected override async Task DecryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    )
    {
        ChaCha20Poly1305 chacha20Poly1305 = new();
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        chacha20Poly1305.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, chacha20Poly1305);
    }
}
