using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Modes;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Services.Encryption;

public abstract class BaseEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : IEncryptionService
{
    protected const int KeySize = 256;
    protected const int SaltSize = 32;
    protected const int NonceSize = 12;
    protected const int TagSize = 16;
    protected const int BufferSize = 4 * 1024;

    public async Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            // Generate a random salt and nonce
            byte[] salt = GenerateSalt();
            byte[] nonce = GenerateNonce();

            // Generate key from password using the specified algorithm
            byte[] key = DeriveKey(password, salt, KeySize, keyDerivationAlgorithm);

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

    public async Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            using FileStream sourceFile = File.OpenRead(sourceFilePath);

            // Read the salt and nonce from the beginning of the file
            byte[] salt = await ReadSaltAsync(sourceFile);
            byte[] nonce = await ReadNonceAsync(sourceFile);

            // Generate key from password and salt using the specified algorithm
            byte[] key = DeriveKey(password, salt, KeySize, keyDerivationAlgorithm);

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

    protected byte[] DeriveKey(string password, byte[] salt, int keySize, KeyDerivationAlgorithm algorithm)
    {
        IKeyDerivationService keyDerivationService = keyDerivationServiceFactory.Create(algorithm);
        return keyDerivationService.DeriveKey(password, salt, keySize);
    }

    protected static async Task WriteSaltAsync(FileStream stream, byte[] salt)
    {
        await stream.WriteAsync(salt);
    }

    protected static async Task<byte[]> ReadSaltAsync(FileStream stream)
    {
        byte[] salt = new byte[SaltSize];
        _ = await stream.ReadAsync(salt);
        return salt;
    }

    protected static async Task WriteNonceAsync(FileStream stream, byte[] nonce)
    {
        await stream.WriteAsync(nonce);
    }

    protected static async Task<byte[]> ReadNonceAsync(FileStream stream)
    {
        byte[] nonce = new byte[NonceSize];
        _ = await stream.ReadAsync(nonce);
        return nonce;
    }

    protected static async Task ProcessFileWithCipherAsync(FileStream sourceStream, FileStream destinationStream, IAeadCipher cipher)
    {
        byte[] inputBuffer = new byte[BufferSize];
        byte[] outputBuffer = new byte[BufferSize];

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