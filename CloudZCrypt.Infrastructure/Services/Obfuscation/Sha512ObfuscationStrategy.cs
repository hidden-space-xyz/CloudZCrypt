using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.Obfuscation;

/// <summary>
/// Provides a name obfuscation strategy that replaces filenames with SHA-512 hash digests of the file content.
/// </summary>
/// <remarks>
/// This strategy creates deterministic obfuscated names based on file content using the SHA-512 algorithm,
/// providing stronger cryptographic properties than SHA-256. Identical files will have identical obfuscated
/// names, but with longer hash values for enhanced security.
/// </remarks>
internal class Sha512ObfuscationStrategy : INameObfuscationStrategy
{
    /// <summary>
    /// Gets the unique identifier representing SHA-512 obfuscation.
    /// </summary>
    public NameObfuscationMode Id => NameObfuscationMode.Sha512;

    /// <summary>
    /// Gets the human-readable display name for this strategy.
    /// </summary>
    public string DisplayName => "SHA-512";

    /// <summary>
    /// Gets a detailed description of this obfuscation strategy.
    /// </summary>
    public string Description =>
        "Replaces the filename with a 128‑character SHA‑512 hexadecimal digest computed from the file content. " +
        "Offers stronger collision resistance than SHA‑256 at the cost of longer names; deterministic across identical content.";

    /// <summary>
    /// Gets a concise summary describing when this strategy is appropriate.
    /// </summary>
    public string Summary => "Best for maximum collision resistance (128-char digest)";

    /// <summary>
    /// Generates a SHA-512 hash-based filename from the file content while preserving the original file extension.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file to hash.</param>
    /// <param name="originalFileName">The original filename (used only for extracting the extension).</param>
    /// <returns>A SHA-512 hash-based filename with the original file extension.</returns>
    /// <exception cref="IOException">Thrown when the source file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the source file is denied.</exception>
    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string hashName = ComputeFileHash(sourceFilePath);
        return hashName + extension;
    }

    /// <summary>
    /// Computes the SHA-512 hash of the specified file content.
    /// </summary>
    /// <param name="filePath">The path to the file to hash.</param>
    /// <returns>The hexadecimal representation of the SHA-512 hash.</returns>
    private static string ComputeFileHash(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = SHA512.HashData(stream);

        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}