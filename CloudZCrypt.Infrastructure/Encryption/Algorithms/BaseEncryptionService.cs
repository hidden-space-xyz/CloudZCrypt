using CloudZCrypt.Application.Interfaces.Encryption;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms;

internal abstract class BaseEncryptionService : IEncryptionService
{
    protected const int SaltSize = 32;
    protected const int Iterations = 100000;
    protected const int KeySize = 256;
    protected const int NonceSize = 12; // 96-bit nonce for GCM/AEAD
    protected const int TagSize = 16; // 128-bit authentication tag
    protected const int BufferSize = 4096;

    public async Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password)
    {
        try
        {
            // Generate a random salt and nonce
            byte[] salt = GenerateSalt();
            byte[] nonce = GenerateNonce();

            // Generate key from password using PBKDF2
            byte[] key = DeriveKey(password, salt, KeySize);

            using FileStream sourceFile = File.OpenRead(sourceFilePath);
            using FileStream destinationFile = File.Create(destinationFilePath);

            // Write salt and nonce at the beginning of the file
            await WriteSaltAsync(destinationFile, salt);
            await WriteNonceAsync(destinationFile, nonce);

            // Perform algorithm-specific encryption
            await EncryptStreamAsync(sourceFile, destinationFile, key, nonce);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password)
    {
        try
        {
            using FileStream sourceFile = File.OpenRead(sourceFilePath);

            // Read the salt and nonce from the beginning of the file
            byte[] salt = await ReadSaltAsync(sourceFile);
            byte[] nonce = await ReadNonceAsync(sourceFile);

            // Generate key from password and salt
            byte[] key = DeriveKey(password, salt, KeySize);

            using FileStream destinationFile = File.Create(destinationFilePath);

            // Perform algorithm-specific decryption
            await DecryptStreamAsync(sourceFile, destinationFile, key, nonce);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    protected abstract Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce);

    protected abstract Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce);

    protected static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }

    protected static byte[] GenerateNonce()
    {
        byte[] nonce = new byte[NonceSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }
        return nonce;
    }

    protected static byte[] DeriveKey(string password, byte[] salt, int keySize)
    {
        using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
        return keyDerivation.GetBytes(keySize / 8);
    }

    protected static byte[] DeriveIv(string password, byte[] salt, int ivSize)
    {
        using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
        keyDerivation.GetBytes(32); // Skip the key bytes
        return keyDerivation.GetBytes(ivSize);
    }

    protected static async Task WriteSaltAsync(FileStream stream, byte[] salt)
    {
        await stream.WriteAsync(salt);
    }

    protected static async Task<byte[]> ReadSaltAsync(FileStream stream)
    {
        byte[] salt = new byte[SaltSize];
        await stream.ReadAsync(salt);
        return salt;
    }

    protected static async Task WriteNonceAsync(FileStream stream, byte[] nonce)
    {
        await stream.WriteAsync(nonce);
    }

    protected static async Task<byte[]> ReadNonceAsync(FileStream stream)
    {
        byte[] nonce = new byte[NonceSize];
        await stream.ReadAsync(nonce);
        return nonce;
    }

    protected static async Task ProcessFileWithCipherAsync(FileStream sourceStream, FileStream destinationStream, dynamic cipher)
    {
        byte[] inputBuffer = new byte[BufferSize];
        byte[] outputBuffer = new byte[cipher.GetOutputSize(BufferSize)];

        int bytesRead;
        while ((bytesRead = await sourceStream.ReadAsync(inputBuffer)) > 0)
        {
            int processed = cipher.ProcessBytes(inputBuffer, 0, bytesRead, outputBuffer, 0);
            if (processed > 0)
            {
                await destinationStream.WriteAsync(outputBuffer.AsMemory(0, processed));
            }
        }

        // Process the final block and get/verify the authentication tag
        int finalBytes = cipher.DoFinal(outputBuffer, 0);
        if (finalBytes > 0)
        {
            await destinationStream.WriteAsync(outputBuffer.AsMemory(0, finalBytes));
        }
    }
}