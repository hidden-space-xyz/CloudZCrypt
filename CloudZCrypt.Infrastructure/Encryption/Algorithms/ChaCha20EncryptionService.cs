using CloudZCrypt.Application.Interfaces.Encryption;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms;

internal class ChaCha20EncryptionService : IEncryptionService
{
    private const int KeySize = 256; // Key size for ChaCha20 (256 bits)
    private const int NonceSize = 8; // Recommended nonce size for ChaCha20
    private const int SaltSize = 32; // Size of the salt for PBKDF2
    private const int Iterations = 100000; // Number of iterations for PBKDF2

    /// <summary>
    /// Encrypts a file using ChaCha20 with a password
    /// </summary>
    /// <param name="sourceFilePath">Path to the file to encrypt</param>
    /// <param name="destinationFilePath">Path where the encrypted file will be saved</param>
    /// <param name="password">Password for encryption</param>
    /// <returns>True if encryption succeeds</returns>
    public async Task<bool> EncryptFileAsync(string sourceFilePath, string destinationFilePath, string password)
    {
        try
        {
            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Generate key from password using PBKDF2
            using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(KeySize / 8);

            // Generate a random nonce
            byte[] nonce = new byte[NonceSize];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Initialize ChaCha20 cipher
            ChaChaEngine chacha20 = new();
            ParametersWithIV keyParam = new(new KeyParameter(key), nonce);
            chacha20.Init(true, keyParam);

            using FileStream sourceFile = File.OpenRead(sourceFilePath);
            using FileStream destinationFile = File.Create(destinationFilePath);

            // Write salt and nonce at the beginning of the file
            await destinationFile.WriteAsync(salt, 0, salt.Length);
            await destinationFile.WriteAsync(nonce, 0, nonce.Length);

            // Process the file in chunks
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await sourceFile.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                byte[] encryptedBuffer = new byte[bytesRead];
                chacha20.ProcessBytes(buffer, 0, bytesRead, encryptedBuffer, 0);
                await destinationFile.WriteAsync(encryptedBuffer, 0, encryptedBuffer.Length);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Decrypts a file that was encrypted using EncryptFile
    /// </summary>
    /// <param name="sourceFilePath">Path to the encrypted file</param>
    /// <param name="destinationFilePath">Path where the decrypted file will be saved</param>
    /// <param name="password">Password used for encryption</param>
    /// <returns>True if decryption succeeds</returns>
    public async Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password)
    {
        try
        {
            using FileStream sourceFile = File.OpenRead(sourceFilePath);

            // Read the salt and nonce from the beginning of the file
            byte[] salt = new byte[SaltSize];
            await sourceFile.ReadAsync(salt, 0, salt.Length);

            byte[] nonce = new byte[NonceSize];
            await sourceFile.ReadAsync(nonce, 0, nonce.Length);

            // Generate key from password and salt
            using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = keyDerivation.GetBytes(KeySize / 8);

            // Initialize ChaCha20 cipher
            ChaChaEngine chacha20 = new();
            ParametersWithIV keyParam = new(new KeyParameter(key), nonce);
            chacha20.Init(false, keyParam);

            using FileStream destinationFile = File.Create(destinationFilePath);

            // Process the file in chunks
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await sourceFile.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                byte[] decryptedBuffer = new byte[bytesRead];
                chacha20.ProcessBytes(buffer, 0, bytesRead, decryptedBuffer, 0);
                await destinationFile.WriteAsync(decryptedBuffer, 0, decryptedBuffer.Length);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}