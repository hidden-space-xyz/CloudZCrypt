namespace CloudZCrypt.Domain.Enums;

/// <summary>
/// Defines the supported key derivation (password-based key stretching) algorithms
/// used to transform a user-supplied secret (e.g., a password or passphrase)
/// into a cryptographic key suitable for encryption or authentication operations.
/// </summary>
/// <remarks>
/// Select an algorithm based on the desired balance between security characteristics,
/// performance, deployment constraints, and interoperability:
/// Ensure that algorithm-specific parameters (iterations, memory size, parallelism, etc.)
/// are selected according to current security guidance and periodically reviewed.
/// </remarks>
public enum KeyDerivationAlgorithm
{
    /// <summary>
    /// Argon2id variant of the Argon2 password hashing function combining data- and address-dependent memory access
    /// to mitigate both side-channel attacks and GPU/ASIC acceleration. Recommended for modern applications when available.
    /// </summary>
    Argon2id,

    /// <summary>
    /// PBKDF2 (Password-Based Key Derivation Function 2) as specified in PKCS #5 (RFC 8018), using iterative HMAC operations.
    /// Offers broad interoperability but provides weaker resistance to massively parallel hardware compared to Argon2id.
    /// </summary>
    PBKDF2,
}
