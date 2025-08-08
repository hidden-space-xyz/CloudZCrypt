using CloudZCrypt.Application.Interfaces.Encryption;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms
{
    internal class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 256; // Key size for AES (256 bits)
        private const int BlockSize = 128; // Block size for AES (128 bits)
        private const int IvSize = 16; // Initialization Vector (IV) size (128 bits / 16 bytes)
        private const int SaltSize = 32; // Size of the salt for PBKDF2
        private const int Iterations = 100000; // Number of iterations for PBKDF2

        /// <summary>
        /// Encrypts a file using AES-256 with a password
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

                // Generate key and IV from password using PBKDF2
                using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize / 8);
                byte[] iv = keyDerivation.GetBytes(IvSize);

                using Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = key;
                aes.IV = iv;

                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                // Write salt at the beginning of the file
                await destinationFile.WriteAsync(salt, 0, salt.Length);

                // Create crypto stream and encrypt
                using CryptoStream cryptoStream = new(destinationFile, aes.CreateEncryptor(), CryptoStreamMode.Write);
                await sourceFile.CopyToAsync(cryptoStream);
                await cryptoStream.FlushFinalBlockAsync();

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

                // Read the salt from the beginning of the file
                byte[] salt = new byte[SaltSize];
                await sourceFile.ReadAsync(salt, 0, salt.Length);

                // Generate key and IV from password and salt
                using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize / 8);
                byte[] iv = keyDerivation.GetBytes(IvSize);

                using Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Key = key;
                aes.IV = iv;

                using FileStream destinationFile = File.Create(destinationFilePath);
                using CryptoStream cryptoStream = new(sourceFile, aes.CreateDecryptor(), CryptoStreamMode.Read);

                await cryptoStream.CopyToAsync(destinationFile);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}