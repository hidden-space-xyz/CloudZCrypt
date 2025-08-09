using CloudZCrypt.Application.Interfaces.Encryption;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms;

internal abstract class BaseEncryptionService : IEncryptionService
{
    protected const int KeySize = 256;
    protected const int SaltSize = 32;
    protected const int NonceSize = 12;
    protected const int TagSize = 16;
    protected const int BufferSize = 4 * 1024;
    protected const int Argon2MemoryCost = 128 * 1024;
    protected const int Argon2Iterations = 5;
    protected const int Argon2Parallelism = 4;

    public async Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password)
    {
        try
        {
            // Generate a random salt and nonce
            byte[] salt = GenerateSalt();
            byte[] nonce = GenerateNonce();

            // Generate key from password using Argon2id
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
        // Create Argon2 generator
        Argon2BytesGenerator argon2 = new();
        argon2.Init(new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithSalt(salt)
            .WithMemoryAsKB(Argon2MemoryCost)
            .WithIterations(Argon2Iterations)
            .WithParallelism(Argon2Parallelism)
            .Build());

        // Generate the key
        byte[] key = new byte[keySize / 8];
        char[]? passwordChars = null;

        try
        {
            // Convert password to character array
            passwordChars = password.ToCharArray();

            // Generate the key
            argon2.GenerateBytes(passwordChars, key);

            return key;
        }
        catch (Exception ex)
        {
            // Cleanup in case of exception
            if (key != null)
                Array.Clear(key, 0, key.Length);

            throw new CryptographicException("Error deriving key", ex);
        }
        finally
        {
            // Clean sensitive data from memory
            if (passwordChars != null)
                Array.Clear(passwordChars, 0, passwordChars.Length);
        }
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