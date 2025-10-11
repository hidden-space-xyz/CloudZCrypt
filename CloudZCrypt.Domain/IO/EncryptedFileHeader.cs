using System.Buffers.Binary;
using System.Text;

namespace CloudZCrypt.Domain.IO;

/// <summary>
/// Defines the lightweight file format header placed after the salt and nonce in encrypted files.
/// The header stores the original filename to enable restoration during decryption, especially when
/// name obfuscation is used.
/// </summary>
public static class EncryptedFileHeader
{
    /// <summary>
    /// Size in bytes of the salt written before this header.
    /// </summary>
    public const int SaltSize = 32;

    /// <summary>
    /// Size in bytes of the nonce (IV) written immediately after the salt and before this header.
    /// </summary>
    public const int NonceSize = 12;

    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CZC1");

    /// <summary>
    /// Writes the original filename metadata at the current position of the stream. The format is:
    /// [4 bytes magic 'CZC1'][4 bytes LE length N][N bytes UTF-8 original file name].
    /// </summary>
    /// <param name="stream">Writable stream positioned where the header should be written.</param>
    /// <param name="originalFileName">The original filename to persist. May be empty but not null.</param>
    public static async Task WriteAsync(Stream stream, string originalFileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        originalFileName ??= string.Empty;

        await stream.WriteAsync(Magic, 0, Magic.Length);

        byte[] nameBytes = Encoding.UTF8.GetBytes(originalFileName);
        byte[] lenBytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(lenBytes, nameBytes.Length);
        await stream.WriteAsync(lenBytes, 0, lenBytes.Length);
        if (nameBytes.Length > 0)
        {
            await stream.WriteAsync(nameBytes, 0, nameBytes.Length);
        }
    }

    /// <summary>
    /// Attempts to read the header from the current stream position. If the magic does not match,
    /// the stream position is restored and null is returned. On success, the stream is left positioned
    /// immediately after the header.
    /// </summary>
    /// <param name="stream">Readable, seekable stream positioned at the expected header start.</param>
    /// <returns>The original filename when present; otherwise null if no header is found.</returns>
    public static async Task<string?> TryReadAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        long start = stream.CanSeek ? stream.Position : 0;

        byte[] magic = new byte[4];
        int read = await stream.ReadAsync(magic, 0, 4);
        if (read < 4 || !magic.AsSpan().SequenceEqual(Magic))
        {
            if (stream.CanSeek)
            {
                stream.Position = start; // rewind when header not present
            }
            return null;
        }

        byte[] lenBytes = new byte[4];
        await stream.ReadExactlyAsync(lenBytes);
        int len = BinaryPrimitives.ReadInt32LittleEndian(lenBytes);
        if (len < 0 || len > 1_000_000)
        {
            // Invalid length: rewind to start of header so caller can decide
            if (stream.CanSeek)
            {
                stream.Position = start;
            }
            return null;
        }

        if (len == 0)
        {
            return string.Empty;
        }

        byte[] nameBytes = new byte[len];
        await stream.ReadExactlyAsync(nameBytes);
        return Encoding.UTF8.GetString(nameBytes);
    }
}
