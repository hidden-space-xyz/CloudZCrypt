using CloudZCrypt.Application.Interfaces.Encryption;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Encryption.Algorithms
{
    internal class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int IvSize = 16;
        private const int SaltSize = 32;
        private const int Iterations = 100000;

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

                // Configure BouncyCastle AES engine
                AesEngine aesEngine = new();
                CbcBlockCipher cbcBlockCipher = new(aesEngine);
                PaddedBufferedBlockCipher paddedBlockCipher = new(cbcBlockCipher, new Pkcs7Padding());
                ParametersWithIV keyParameter = new(new KeyParameter(key), iv);
                paddedBlockCipher.Init(true, keyParameter);

                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                // Write salt at the beginning of the file
                await destinationFile.WriteAsync(salt);

                // Process the file in chunks
                const int bufferSize = 4096;
                byte[] inputBuffer = new byte[bufferSize];
                byte[] outputBuffer = new byte[paddedBlockCipher.GetOutputSize(bufferSize)];

                int bytesRead;
                while ((bytesRead = await sourceFile.ReadAsync(inputBuffer)) > 0)
                {
                    int processed = paddedBlockCipher.ProcessBytes(inputBuffer, 0, bytesRead, outputBuffer, 0);
                    await destinationFile.WriteAsync(outputBuffer.AsMemory(0, processed));
                }

                // Process the final block
                int finalBytes = paddedBlockCipher.DoFinal(outputBuffer, 0);
                if (finalBytes > 0)
                {
                    await destinationFile.WriteAsync(outputBuffer.AsMemory(0, finalBytes));
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
                await sourceFile.ReadAsync(salt);

                // Generate key and IV from password and salt
                using Rfc2898DeriveBytes keyDerivation = new(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] key = keyDerivation.GetBytes(KeySize / 8);
                byte[] iv = keyDerivation.GetBytes(IvSize);

                // Configure BouncyCastle AES engine
                AesEngine aesEngine = new();
                CbcBlockCipher cbcBlockCipher = new(aesEngine);
                PaddedBufferedBlockCipher paddedBlockCipher = new(cbcBlockCipher, new Pkcs7Padding());
                ParametersWithIV keyParameter = new(new KeyParameter(key), iv);
                paddedBlockCipher.Init(false, keyParameter);

                using FileStream destinationFile = File.Create(destinationFilePath);

                // Process the file in chunks
                const int bufferSize = 4096;
                byte[] inputBuffer = new byte[bufferSize];
                byte[] outputBuffer = new byte[paddedBlockCipher.GetOutputSize(bufferSize)];

                int bytesRead;
                while ((bytesRead = await sourceFile.ReadAsync(inputBuffer)) > 0)
                {
                    int processed = paddedBlockCipher.ProcessBytes(inputBuffer, 0, bytesRead, outputBuffer, 0);
                    await destinationFile.WriteAsync(outputBuffer.AsMemory(0, processed));
                }

                // Process the final block
                int finalBytes = paddedBlockCipher.DoFinal(outputBuffer, 0);
                if (finalBytes > 0)
                {
                    await destinationFile.WriteAsync(outputBuffer.AsMemory(0, finalBytes));
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