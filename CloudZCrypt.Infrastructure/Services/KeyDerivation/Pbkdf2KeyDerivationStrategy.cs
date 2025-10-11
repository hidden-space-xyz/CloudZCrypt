using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.KeyDerivation;

/// <summary>
/// Implements the PBKDF2 (Password-Based Key Derivation Function 2) algorithm using
/// HMAC-SHA256 to derive cryptographic keys from user-supplied passwords and salts.
/// </summary>
/// <remarks>
/// This strategy provides a compatibility-focused key derivation mechanism compliant with
/// PKCS #5 (RFC 8018) and commonly approved in regulated environments that mandate FIPS-aligned
/// primitives. While PBKDF2 lacks intrinsic memory hardness (unlike Argon2id or scrypt) and is more
/// susceptible to acceleration on specialized hardware (GPU / ASIC), sufficiently high iteration counts
/// still make brute-force attempts costly. Use this implementation when interoperability or compliance
/// outweighs the benefits of newer memory‑hard schemes.
/// </remarks>
internal class Pbkdf2KeyDerivationStrategy : IKeyDerivationAlgorithmStrategy
{
    /// <summary>
    /// The iteration count (work factor) applied when expanding the password.
    /// Higher values increase computational cost for both legitimate and adversarial use.
    /// </summary>
    private const int Iterations = 800000;

    /// <summary>
    /// Gets the enumeration identifier representing the PBKDF2 algorithm.
    /// </summary>
    public KeyDerivationAlgorithm Id => KeyDerivationAlgorithm.PBKDF2;

    /// <summary>
    /// Gets a short, user-friendly display name for the algorithm.
    /// </summary>
    public string DisplayName => "PBKDF2 (HMAC-SHA256)";

    /// <summary>
    /// Gets a detailed description outlining characteristics, strengths, and trade-offs of PBKDF2.
    /// </summary>
    public string Description =>
        "A widely standardized (PKCS #5, RFC 8018, FIPS 140 allowed) iterative, CPU‑bound key derivation function using repeated HMAC-SHA256 applications. " +
        "Simple and broadly implemented in virtually all cryptographic libraries. " +
        "Lacks intrinsic memory hardness, making it comparatively cheaper to accelerate on GPUs/ASICs versus Argon2id or scrypt. " +
        "Still appropriate where regulatory, legacy platform, or FIPS compliance requirements dominate, or when only conservative primitives are permitted. " +
        "Security hinges on high iteration counts (cost parameter) and high‑entropy passwords.";

    /// <summary>
    /// Gets a concise summary emphasizing the primary suitability of this algorithm.
    /// </summary>
    public string Summary => "Best for maximum compatibility / legacy & compliance needs";

    /// <summary>
    /// Derives a cryptographic key of the specified length (in bytes) from the provided password and salt
    /// using the PBKDF2 algorithm with HMAC-SHA256 and a fixed iteration count.
    /// </summary>
    /// <param name="password">The user-supplied secret (passphrase). Must not be null or empty.</param>
    /// <param name="salt">A cryptographically strong, unique salt. Must not be null and should be at least 16 bytes.</param>
    /// <param name="keySize">The desired key length in bytes (e.g., 32 for a 256-bit key). Must be a positive integer.</param>
    /// <returns>A newly allocated byte array containing the derived key material of the requested length.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="password"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="password"/> is empty, <paramref name="salt"/> is empty, or <paramref name="keySize"/> is not positive.</exception>
    /// <exception cref="CryptographicException">Thrown when an error occurs during key derivation or underlying cryptographic processing.</exception>
    public byte[] DeriveKey(string password, byte[] salt, int keySize)
    {
        byte[]? passwordBytes = null;
        byte[]? key = null;

        try
        {
            passwordBytes = Encoding.UTF8.GetBytes(password);

            Pkcs5S2ParametersGenerator pbkdf2 = new();
            pbkdf2.Init(passwordBytes, salt, Iterations);

            KeyParameter keyParam = (KeyParameter)pbkdf2.GenerateDerivedMacParameters(keySize);
            key = keyParam.GetKey();

            byte[] result = new byte[key.Length];
            Array.Copy(key, result, key.Length);
            return result;
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Error deriving key with PBKDF2", ex);
        }
        finally
        {
            if (passwordBytes != null)
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }

            if (key != null)
            {
                Array.Clear(key, 0, key.Length);
            }
        }
    }
}
