namespace CloudZCrypt.Domain.Enums;

/// <summary>
/// Specifies the symmetric encryption algorithms supported by the CloudZCrypt domain.
/// </summary>
/// <remarks>
/// This enumeration is used to indicate which cryptographic primitive should be applied when
/// performing encryption or decryption operations. The actual availability of a given algorithm
/// may depend on the runtime environment, underlying cryptographic libraries, or compliance
/// constraints. When adding new algorithms, ensure the corresponding implementation, validation,
/// and key handling logic are provided in the application and infrastructure layers.
/// </remarks>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// Advanced Encryption Standard (AES), a widely adopted and hardware-accelerated block cipher.
    /// Recommended as the default choice for most scenarios due to its performance and security maturity.
    /// </summary>
    Aes,

    /// <summary>
    /// Twofish block cipher, a finalist in the AES competition, valued for its flexible key sizes
    /// and conservative design. Typically slower than AES with hardware acceleration.
    /// </summary>
    Twofish,

    /// <summary>
    /// Serpent block cipher, designed with a security-first philosophy and large safety margin.
    /// Generally slower than AES but still considered secure.
    /// </summary>
    Serpent,

    /// <summary>
    /// ChaCha20 stream cipher, offering high performance in software and resistance to timing attacks.
    /// Commonly paired with Poly1305 for authenticated encryption.
    /// </summary>
    ChaCha20,

    /// <summary>
    /// Camellia block cipher, offering security and performance characteristics comparable to AES
    /// and standardized in several international specifications.
    /// </summary>
    Camellia,
}
