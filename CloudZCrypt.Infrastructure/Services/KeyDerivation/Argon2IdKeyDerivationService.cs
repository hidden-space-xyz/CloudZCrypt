using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Services.KeyDerivation;

/// <summary>
/// Provides an <see cref="IKeyDerivationAlgorithmStrategy"/> implementation that derives cryptographic keys
/// using the Argon2id password hashing / key derivation function.
/// </summary>
/// <remarks>
/// Argon2id is the hybrid variant of the Argon2 family (Password Hashing Competition winner) combining the
/// side‑channel resistance of Argon2i with the GPU/ASIC resistance of Argon2d. This strategy encapsulates
/// opinionated, security‑oriented defaults (memory cost, iterations, and parallelism) chosen to impose
/// substantial computational and memory load on attackers while remaining practical for legitimate use.
/// </remarks>
public class Argon2IdKeyDerivationService : IKeyDerivationAlgorithmStrategy
{
    // Memory cost in KB (128 MB) for GPU resistance
    private const int MemoryCost = 128 * 1024;

    // Number of iterations (time cost)
    private const int Iterations = 5;

    // Parallel lanes (degree of parallelism)
    private const int Parallelism = 4;

    /// <summary>
    /// Gets the unique algorithm identifier representing Argon2id.
    /// </summary>
    public KeyDerivationAlgorithm Id => KeyDerivationAlgorithm.Argon2id;

    /// <summary>
    /// Gets a short, user‑friendly display name for the algorithm.
    /// </summary>
    public string DisplayName => "Argon2id";

    /// <summary>
    /// Gets a detailed description of the Argon2id algorithm, its security properties, and tuning dimensions.
    /// </summary>
    public string Description =>
        "A modern memory‑hard password hashing and key derivation function (PHC winner). The “id” variant blends Argon2i (side‑channel resistant) and Argon2d (GPU/ASIC resistance) for balanced security. Tunable via: memory cost (m), iterations/time cost (t), and parallelism (p). Provides strong resistance to large‑scale brute force on GPUs, FPGAs, and ASICs by imposing substantial RAM requirements. Supports domain separation with distinct salt plus optional secret/data parameters. Recommended where modern security is prioritized over legacy compatibility.";

    /// <summary>
    /// Gets a concise summary emphasizing the primary strength of Argon2id.
    /// </summary>
    public string Summary => "Best for maximum security / modern memory‑hard password hashing";

    /// <summary>
    /// Derives a cryptographic key from the supplied password and salt using Argon2id with predefined
    /// memory, time, and parallelism parameters.
    /// </summary>
    /// <param name="password">The user‑supplied passphrase. Must not be null or empty.</param>
    /// <param name="salt">A cryptographically strong, unique salt (recommended minimum 16 bytes). Must not be null.</param>
    /// <param name="keySize">The desired key size in bits. Must be a positive multiple of 8 suitable for the target cipher.</param>
    /// <returns>A byte array containing the derived key of length <paramref name="keySize"/> / 8.</returns>
    /// <exception cref="CryptographicException">Thrown when the underlying Argon2 computation fails.</exception>
    /// <remarks>
    /// Input validation (null/empty checks) is expected to be enforced by higher‑level components per the
    /// <see cref="IKeyDerivationAlgorithmStrategy"/> contract. The method securely clears sensitive temporary
    /// buffers (password characters and derived key intermediate) before completion.
    /// </remarks>
    public byte[] DeriveKey(string password, byte[] salt, int keySize)
    {
        Argon2BytesGenerator argon2 = new();
        argon2.Init(
            new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
                .WithSalt(salt)
                .WithMemoryAsKB(MemoryCost)
                .WithIterations(Iterations)
                .WithParallelism(Parallelism)
                .Build()
        );

        byte[] key = new byte[keySize / 8];
        char[]? passwordChars = null;

        try
        {
            passwordChars = password.ToCharArray();
            argon2.GenerateBytes(passwordChars, key);
            return key;
        }
        catch (Exception ex)
        {
            if (key != null)
            {
                Array.Clear(key, 0, key.Length);
            }

            throw new CryptographicException("Error deriving key with Argon2id", ex);
        }
        finally
        {
            if (passwordChars != null)
            {
                Array.Clear(passwordChars, 0, passwordChars.Length);
            }
        }
    }
}
