using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.Obfuscation;

internal class Sha256ObfuscationStrategy : INameObfuscationStrategy
{
    public NameObfuscationMode Id => NameObfuscationMode.Sha256;

    public string DisplayName => "SHA-256";

    public string Description =>
        "Replaces the filename with a 64‑character SHA‑256 hexadecimal digest computed from the file content. " +
        "Deterministic across identical content, enabling deduplication and content-addressable naming without leaking the original filename.";

    public string Summary => "Best for content-addressed naming (64-char digest)";

    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string hashName = ComputeFileHash(sourceFilePath);
        return hashName + extension;
    }

    private static string ComputeFileHash(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = SHA256.HashData(stream);

        StringBuilder sb = new(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}