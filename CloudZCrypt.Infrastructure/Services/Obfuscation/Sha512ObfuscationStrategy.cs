using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.Obfuscation;

internal class Sha512ObfuscationStrategy : INameObfuscationStrategy
{
    public NameObfuscationMode Id => NameObfuscationMode.Sha512;

    public string DisplayName => "SHA-512";

    public string Description =>
        "Replaces the filename with a 128‑character SHA‑512 hexadecimal digest computed from the file content. "
        + "Offers stronger collision resistance than SHA‑256 at the cost of longer names; deterministic across identical content.";

    public string Summary => "Best for maximum collision resistance (128-char digest)";

    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string hashName = ComputeFileHash(sourceFilePath);
        return hashName + extension;
    }

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
