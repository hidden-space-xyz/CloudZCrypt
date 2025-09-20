using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Exceptions;
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
            // Validate input files
            if (!File.Exists(sourceFilePath))
            {
                throw new EncryptionFileNotFoundException(sourceFilePath);
            }

            // Check if we can read the source file
            try
            {
                using FileStream testRead = File.OpenRead(sourceFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(sourceFilePath, ex);
            }

            // Check if we can write to the destination
            string? destinationDir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                try
                {
                    Directory.CreateDirectory(destinationDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new EncryptionAccessDeniedException(destinationFilePath, ex);
                }
            }

            // Check disk space
            await ValidateDiskSpaceAsync(sourceFilePath, destinationFilePath);

            // Generate cryptographic materials
            byte[] salt = GenerateSalt();
            byte[] nonce = GenerateNonce();

            // Derive key
            byte[] key;
            try
            {
                key = DeriveKey(password, salt, KeySize, keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {
                throw new EncryptionKeyDerivationException(ex);
            }

            // Perform encryption
            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                await WriteSaltAsync(destinationFile, salt);
                await WriteNonceAsync(destinationFile, nonce);
                await EncryptStreamAsync(sourceFile, destinationFile, key, nonce);
            }
            catch (IOException ex)
            {
                // Clean up partial file on failure
                try
                {
                    if (File.Exists(destinationFilePath))
                        File.Delete(destinationFilePath);
                }
                catch { /* Ignore cleanup errors */ }

                if (ex.Message.Contains("space", StringComparison.OrdinalIgnoreCase))
                {
                    throw new EncryptionInsufficientSpaceException(destinationFilePath);
                }
                throw new EncryptionCipherException("encryption", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(destinationFilePath, ex);
            }
            catch (Exception ex)
            {
                // Clean up partial file on failure
                try
                {
                    if (File.Exists(destinationFilePath))
                        File.Delete(destinationFilePath);
                }
                catch { /* Ignore cleanup errors */ }

                throw new EncryptionCipherException("encryption", ex);
            }

            return true;
        }
        catch (EncryptionException)
        {
            throw; // Re-throw our specific exceptions
        }
        catch (Exception ex)
        {
            throw new EncryptionCipherException("encryption", ex);
        }
    }

    public async Task<bool> DecryptFileAsync(string sourceFilePath, string destinationFilePath, string password, KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            // Validate input files
            if (!File.Exists(sourceFilePath))
            {
                throw new EncryptionFileNotFoundException(sourceFilePath);
            }

            // Check if we can read the source file
            try
            {
                using FileStream testRead = File.OpenRead(sourceFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(sourceFilePath, ex);
            }

            // Validate file size (must be at least salt + nonce size)
            FileInfo fileInfo = new(sourceFilePath);
            if (fileInfo.Length < SaltSize + NonceSize)
            {
                throw new EncryptionCorruptedFileException(sourceFilePath);
            }

            // Check if we can write to the destination
            string? destinationDir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                try
                {
                    Directory.CreateDirectory(destinationDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new EncryptionAccessDeniedException(destinationFilePath, ex);
                }
            }

            // Read cryptographic materials and derive key
            byte[] salt, nonce, key;
            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                salt = await ReadSaltAsync(sourceFile);
                nonce = await ReadNonceAsync(sourceFile);

                // Reset to beginning and validate we read correctly
                sourceFile.Seek(0, SeekOrigin.Begin);
                await sourceFile.ReadExactlyAsync(new byte[SaltSize + NonceSize]);
            }
            catch (EndOfStreamException)
            {
                throw new EncryptionCorruptedFileException(sourceFilePath);
            }
            catch (Exception ex)
            {
                throw new EncryptionCorruptedFileException(sourceFilePath);
            }

            try
            {
                key = DeriveKey(password, salt, KeySize, keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {
                throw new EncryptionKeyDerivationException(ex);
            }

            // Perform decryption
            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                // Skip salt and nonce
                await sourceFile.ReadAsync(new byte[SaltSize + NonceSize]);

                await DecryptStreamAsync(sourceFile, destinationFile, key, nonce);
            }
            catch (IOException ex)
            {
                // Clean up partial file on failure
                try
                {
                    if (File.Exists(destinationFilePath))
                        File.Delete(destinationFilePath);
                }
                catch { /* Ignore cleanup errors */ }

                if (ex.Message.Contains("space", StringComparison.OrdinalIgnoreCase))
                {
                    throw new EncryptionInsufficientSpaceException(destinationFilePath);
                }
                throw new EncryptionCipherException("decryption", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(destinationFilePath, ex);
            }
            catch (CryptographicException ex)
            {
                // This typically indicates wrong password or corrupted data
                throw new EncryptionInvalidPasswordException();
            }
            catch (Exception ex)
            {
                // Clean up partial file on failure
                try
                {
                    if (File.Exists(destinationFilePath))
                        File.Delete(destinationFilePath);
                }
                catch { /* Ignore cleanup errors */ }

                // Check if it might be a password issue
                if (ex.Message.Contains("tag", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("mac", StringComparison.OrdinalIgnoreCase))
                {
                    throw new EncryptionInvalidPasswordException();
                }

                throw new EncryptionCipherException("decryption", ex);
            }

            return true;
        }
        catch (EncryptionException)
        {
            throw; // Re-throw our specific exceptions
        }
        catch (Exception ex)
        {
            throw new EncryptionCipherException("decryption", ex);
        }
    }

    private static async Task ValidateDiskSpaceAsync(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            FileInfo sourceFileInfo = new(sourceFilePath);
            string? destinationDrive = Path.GetPathRoot(Path.GetFullPath(destinationFilePath));

            if (!string.IsNullOrEmpty(destinationDrive))
            {
                DriveInfo driveInfo = new(destinationDrive);
                if (driveInfo.IsReady)
                {
                    // Estimate required space (source file size + 20% buffer + fixed overhead)
                    long requiredSpace = (long)(sourceFileInfo.Length * 1.2) + 1024; // 20% buffer + 1KB overhead

                    if (driveInfo.AvailableFreeSpace < requiredSpace)
                    {
                        throw new EncryptionInsufficientSpaceException(destinationFilePath);
                    }
                }
            }
        }
        catch (EncryptionException)
        {
            throw;
        }
        catch
        {
            // If we can't check disk space, continue anyway
        }

        await Task.CompletedTask;
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
        await stream.ReadExactlyAsync(salt);
        return salt;
    }

    protected static async Task WriteNonceAsync(FileStream stream, byte[] nonce)
    {
        await stream.WriteAsync(nonce);
    }

    protected static async Task<byte[]> ReadNonceAsync(FileStream stream)
    {
        byte[] nonce = new byte[NonceSize];
        await stream.ReadExactlyAsync(nonce);
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

        int finalBytes = cipher.DoFinal(outputBuffer, 0);
        if (finalBytes > 0)
        {
            await destinationStream.WriteAsync(outputBuffer.AsMemory(0, finalBytes));
        }
    }
}
