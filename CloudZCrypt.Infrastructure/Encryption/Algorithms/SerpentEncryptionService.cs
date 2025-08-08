using CloudZCrypt.Application.Interfaces.Encryption;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms
{
    internal class SerpentEncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int IvSize = 16;
        private const int SaltSize = 32;
        private const int Iterations = 100000;

        /// <summary>
        /// Encrypts a file using Serpent-256 with a password
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

                // Create Serpent engine
                SerpentEngine serpentEngine = new();
                CbcBlockCipher cbcBlockCipher = new(serpentEngine);
                ParametersWithIV keyParam = new(new KeyParameter(key), iv);
                cbcBlockCipher.Init(true, keyParam);

                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                // Write salt at the beginning of the file
                await destinationFile.WriteAsync(salt, 0, salt.Length);

                // Process the file in blocks
                int blockSize = BlockSize / 8;
                byte[] inputBuffer = new byte[blockSize];
                byte[] outputBuffer = new byte[blockSize];

                int bytesRead;
                while ((bytesRead = await sourceFile.ReadAsync(inputBuffer, 0, blockSize)) > 0)
                {
                    if (bytesRead < blockSize)
                    {
                        // Apply PKCS7 padding
                        int paddingLength = blockSize - bytesRead;
                        for (int i = bytesRead; i < blockSize; i++)
                        {
                            inputBuffer[i] = (byte)paddingLength;
                        }
                    }

                    cbcBlockCipher.ProcessBlock(inputBuffer, 0, outputBuffer, 0);
                    await destinationFile.WriteAsync(outputBuffer, 0, blockSize);
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

                // Read the salt from the beginning of the file
                byte[] salt = new byte[SaltSize];
                await sourceFile.ReadAsync(salt, 0, salt.Length);

                // Generate key and IV from password and salt
                using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize / 8);
                byte[] iv = keyDerivation.GetBytes(IvSize);

                // Create Serpent engine
                SerpentEngine serpentEngine = new();
                CbcBlockCipher cbcBlockCipher = new(serpentEngine);
                ParametersWithIV keyParam = new(new KeyParameter(key), iv);
                cbcBlockCipher.Init(false, keyParam);

                using FileStream destinationFile = File.Create(destinationFilePath);

                // Process the file in blocks
                int blockSize = BlockSize / 8;
                byte[] inputBuffer = new byte[blockSize];
                byte[] outputBuffer = new byte[blockSize];
                long totalBytes = sourceFile.Length - SaltSize;
                long processedBytes = 0;

                while (processedBytes < totalBytes)
                {
                    await sourceFile.ReadAsync(inputBuffer, 0, blockSize);
                    cbcBlockCipher.ProcessBlock(inputBuffer, 0, outputBuffer, 0);

                    processedBytes += blockSize;

                    if (processedBytes >= totalBytes)
                    {
                        // Handle PKCS7 padding in the last block
                        int paddingLength = outputBuffer[blockSize - 1];
                        if (paddingLength > 0 && paddingLength <= blockSize)
                        {
                            await destinationFile.WriteAsync(outputBuffer, 0, blockSize - paddingLength);
                        }
                    }
                    else
                    {
                        await destinationFile.WriteAsync(outputBuffer, 0, blockSize);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}