using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Exceptions;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.IO;
using CloudZCrypt.Domain.Strategies.Interfaces;
using Org.BouncyCastle.Crypto.Modes;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Services.Encryption;

/// <summary>
/// Provides a common, high-level framework for performing authenticated file encryption and decryption
/// using password-derived keys and AEAD (Authenticated Encryption with Associated Data) ciphers.
/// </summary>
/// <remarks>
/// This abstract base class encapsulates cross-cutting concerns such as input validation, disk space
/// preflight checks, key derivation, salt and nonce handling, streaming I/O, and structured exception
/// translation into domain-specific <see cref="EncryptionException"/> types.
/// </remarks>
/// <param name="keyDerivationServiceFactory">Factory used to resolve a concrete key derivation strategy based on the selected <see cref="KeyDerivationAlgorithm"/>.</param>
internal abstract class EncryptionStrategyBase(IKeyDerivationServiceFactory keyDerivationServiceFactory)
{
    /// <summary>
    /// Size (in bits) of the symmetric encryption key produced from password derivation.
    /// </summary>
    protected const int KeySize = 256;

    /// <summary>
    /// Size (in bytes) of the randomly generated salt prepended to encrypted files to enable key derivation during decryption.
    /// </summary>
    protected const int SaltSize = 32;

    /// <summary>
    /// Size (in bytes) of the nonce (a.k.a. IV) used per encryption operation to ensure uniqueness and prevent replay.
    /// </summary>
    protected const int NonceSize = 12;

    /// <summary>
    /// Size (in bits) of the authentication tag (MAC) produced by AEAD ciphers.
    /// </summary>
    protected const int MacSize = 128;

    /// <summary>
    /// Size (in bytes) of the I/O buffer used for streaming encryption and decryption operations.
    /// </summary>
    protected const int BufferSize = 4 * 1024;

    /// <summary>
    /// Encrypts a file using an AEAD cipher implementation provided by a derived class, writing salt and nonce metadata
    /// ahead of the ciphertext so the operation can be reversed with the same password and algorithm selection.
    /// </summary>
    /// <param name="sourceFilePath">Full path to the plaintext source file. Must exist and be readable.</param>
    /// <param name="destinationFilePath">Full path where the encrypted file will be created or overwritten.</param>
    /// <param name="password">User-supplied secret used for key derivation. Must not be null or empty.</param>
    /// <param name="keyDerivationAlgorithm">The key derivation algorithm to use when transforming the password into a fixed-length key.</param>
    /// <returns>true if encryption completes successfully.</returns>
    /// <exception cref="EncryptionFileNotFoundException">Thrown when the source file does not exist.</exception>
    /// <exception cref="EncryptionAccessDeniedException">Thrown when read or write access is denied for source or destination paths.</exception>
    /// <exception cref="EncryptionInsufficientSpaceException">Thrown when the destination drive lacks sufficient free space.</exception>
    /// <exception cref="EncryptionKeyDerivationException">Thrown when key derivation fails (e.g., unsupported algorithm or internal error).</exception>
    /// <exception cref="EncryptionCipherException">Thrown when a lower-level cryptographic or I/O failure occurs.</exception>
    public async Task<bool> EncryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string password,
        KeyDerivationAlgorithm keyDerivationAlgorithm
    )
    {
        try
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new EncryptionFileNotFoundException(sourceFilePath);
            }

            try
            {
                using FileStream testRead = File.OpenRead(sourceFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(sourceFilePath, ex);
            }

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

            await ValidateDiskSpaceAsync(sourceFilePath, destinationFilePath);

            byte[] salt = GenerateSalt();
            byte[] nonce = GenerateNonce();

            byte[] key;
            try
            {
                key = DeriveKey(password, salt, KeySize, keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {
                throw new EncryptionKeyDerivationException(ex);
            }

            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                await WriteSaltAsync(destinationFile, salt);
                await WriteNonceAsync(destinationFile, nonce);
                // Write a small header containing the original filename; downstream logic can choose to use it
                string originalName = Path.GetFileName(sourceFilePath);
                await EncryptedFileHeader.WriteAsync(destinationFile, originalName);

                await EncryptStreamAsync(sourceFile, destinationFile, key, nonce);
            }
            catch (IOException ex)
            {
                try
                {
                    if (File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                }
                catch
                {
                    /* Ignore cleanup errors */
                }

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
                try
                {
                    if (File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                }
                catch
                {
                    /* Ignore cleanup errors */
                }

                throw new EncryptionCipherException("encryption", ex);
            }

            return true;
        }
        catch (EncryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EncryptionCipherException("encryption", ex);
        }
    }

    /// <summary>
    /// Decrypts a file previously produced by <see cref="EncryptFileAsync"/>, restoring the original plaintext.
    /// Validates basic structural integrity (presence of salt and nonce) and derives the appropriate key before invoking
    /// the cipher-specific stream decryption implementation supplied by a derived class.
    /// </summary>
    /// <param name="sourceFilePath">Full path to the encrypted source file. Must exist and be readable.</param>
    /// <param name="destinationFilePath">Full path where the decrypted plaintext file will be written or overwritten.</param>
    /// <param name="password">Password used originally for encryption; must match to authenticate and decrypt.</param>
    /// <param name="keyDerivationAlgorithm">The key derivation algorithm originally used during encryption.</param>
    /// <returns>true if decryption completes successfully.</returns>
    /// <exception cref="EncryptionFileNotFoundException">Thrown when the encrypted source file does not exist.</exception>
    /// <exception cref="EncryptionAccessDeniedException">Thrown when read or write access is denied.</exception>
    /// <exception cref="EncryptionCorruptedFileException">Thrown when the file is too small or structured metadata cannot be read.</exception>
    /// <exception cref="EncryptionInvalidPasswordException">Thrown when authentication fails (e.g., wrong password or tampered data).</exception>
    /// <exception cref="EncryptionInsufficientSpaceException">Thrown when the destination drive lacks required free space.</exception>
    /// <exception cref="EncryptionKeyDerivationException">Thrown when key derivation fails for the supplied password and parameters.</exception>
    /// <exception cref="EncryptionCipherException">Thrown for other unexpected I/O or cryptographic failures.</exception>
    public async Task<bool> DecryptFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        string password,
        KeyDerivationAlgorithm keyDerivationAlgorithm
    )
    {
        try
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new EncryptionFileNotFoundException(sourceFilePath);
            }

            try
            {
                using FileStream testRead = File.OpenRead(sourceFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new EncryptionAccessDeniedException(sourceFilePath, ex);
            }

            FileInfo fileInfo = new(sourceFilePath);
            if (fileInfo.Length < SaltSize + NonceSize)
            {
                throw new EncryptionCorruptedFileException(sourceFilePath);
            }

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

            byte[] salt,
                nonce,
                key;
            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                salt = await ReadSaltAsync(sourceFile);
                nonce = await ReadNonceAsync(sourceFile);

                sourceFile.Seek(0, SeekOrigin.Begin);
                await sourceFile.ReadExactlyAsync(new byte[SaltSize + NonceSize]);
            }
            catch (EndOfStreamException)
            {
                throw new EncryptionCorruptedFileException(sourceFilePath);
            }
            catch
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

            try
            {
                using FileStream sourceFile = File.OpenRead(sourceFilePath);
                using FileStream destinationFile = File.Create(destinationFilePath);

                await sourceFile.ReadExactlyAsync(new byte[SaltSize + NonceSize]);
                // Consume optional header (if present) before cipher data
                _ = await EncryptedFileHeader.TryReadAsync(sourceFile);

                await DecryptStreamAsync(sourceFile, destinationFile, key, nonce);
            }
            catch (IOException ex)
            {
                try
                {
                    if (File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                }
                catch
                {
                    /* Ignore cleanup errors */
                }

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
            catch (CryptographicException)
            {
                throw new EncryptionInvalidPasswordException();
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                }
                catch
                {
                    /* Ignore cleanup errors */
                }

                if (
                    ex.Message.Contains("tag", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("mac", StringComparison.OrdinalIgnoreCase)
                )
                {
                    throw new EncryptionInvalidPasswordException();
                }

                throw new EncryptionCipherException("decryption", ex);
            }

            return true;
        }
        catch (EncryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EncryptionCipherException("decryption", ex);
        }
    }

    /// <summary>
    /// Encrypts data from a source stream into a destination stream using the provided derived key and nonce.
    /// Implemented by derived classes to supply the concrete AEAD cipher mechanics.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing plaintext input.</param>
    /// <param name="destinationStream">Writable stream receiving ciphertext output.</param>
    /// <param name="key">Symmetric key derived from the user password.</param>
    /// <param name="nonce">Per-encryption unique nonce/IV.</param>
    /// <returns>A task representing the asynchronous encryption operation.</returns>
    protected abstract Task EncryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    );

    /// <summary>
    /// Decrypts data from a source stream into a destination stream using the provided derived key and nonce.
    /// Implemented by derived classes to supply the concrete AEAD cipher mechanics and authentication checks.
    /// </summary>
    /// <param name="sourceStream">Readable stream containing ciphertext input.</param>
    /// <param name="destinationStream">Writable stream receiving plaintext output.</param>
    /// <param name="key">Symmetric key derived from the user password.</param>
    /// <param name="nonce">Nonce/IV extracted from the encrypted file header.</param>
    /// <returns>A task representing the asynchronous decryption operation.</returns>
    protected abstract Task DecryptStreamAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        byte[] key,
        byte[] nonce
    );

    /// <summary>
    /// Generates a cryptographically strong random salt suitable for password-based key derivation.
    /// </summary>
    /// <returns>A newly generated salt byte array of length <see cref="SaltSize"/>.</returns>
    protected static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }

    /// <summary>
    /// Generates a cryptographically strong random nonce (initialization vector) for use with an AEAD cipher.
    /// </summary>
    /// <returns>A newly generated nonce byte array of length <see cref="NonceSize"/>.</returns>
    protected static byte[] GenerateNonce()
    {
        byte[] nonce = new byte[NonceSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }
        return nonce;
    }

    /// <summary>
    /// Derives a symmetric key from the supplied password and salt using the selected algorithm strategy.
    /// </summary>
    /// <param name="password">User-provided secret. Must not be null or empty.</param>
    /// <param name="salt">Random salt used to introduce uniqueness and thwart precomputation attacks.</param>
    /// <param name="keySize">Desired key size in bits.</param>
    /// <param name="algorithm">The key derivation algorithm to employ.</param>
    /// <returns>The derived key as a byte array of the requested size.</returns>
    /// <exception cref="EncryptionKeyDerivationException">Indirectly thrown when algorithm strategy fails (wrapped higher up).</exception>
    protected byte[] DeriveKey(
        string password,
        byte[] salt,
        int keySize,
        KeyDerivationAlgorithm algorithm
    )
    {
        IKeyDerivationAlgorithmStrategy keyDerivationService = keyDerivationServiceFactory.Create(
            algorithm
        );
        return keyDerivationService.DeriveKey(password, salt, keySize);
    }

    /// <summary>
    /// Writes the salt to the destination stream as part of the file header.
    /// </summary>
    /// <param name="stream">Writable file stream representing the encryption output target.</param>
    /// <param name="salt">Salt bytes to persist.</param>
    protected static async Task WriteSaltAsync(FileStream stream, byte[] salt)
    {
        await stream.WriteAsync(salt);
    }

    /// <summary>
    /// Reads the salt from the current position in the supplied stream.
    /// </summary>
    /// <param name="stream">Readable file stream positioned at the start of the salt.</param>
    /// <returns>The salt byte array.</returns>
    protected static async Task<byte[]> ReadSaltAsync(FileStream stream)
    {
        byte[] salt = new byte[SaltSize];
        await stream.ReadExactlyAsync(salt);
        return salt;
    }

    /// <summary>
    /// Writes the nonce to the destination stream following the salt in the file header.
    /// </summary>
    /// <param name="stream">Writable file stream representing the encryption output target.</param>
    /// <param name="nonce">Nonce bytes to persist.</param>
    protected static async Task WriteNonceAsync(FileStream stream, byte[] nonce)
    {
        await stream.WriteAsync(nonce);
    }

    /// <summary>
    /// Reads the nonce from the current position in the supplied stream.
    /// </summary>
    /// <param name="stream">Readable file stream positioned at the start of the nonce.</param>
    /// <returns>The nonce byte array.</returns>
    protected static async Task<byte[]> ReadNonceAsync(FileStream stream)
    {
        byte[] nonce = new byte[NonceSize];
        await stream.ReadExactlyAsync(nonce);
        return nonce;
    }

    /// <summary>
    /// Processes the entire contents of the source stream through the provided AEAD cipher in a buffered manner,
    /// writing the resulting output (ciphertext or plaintext) to the destination stream. Finalizes the cipher to
    /// flush any buffered state and authentication tag.
    /// </summary>
    /// <param name="sourceStream">Readable input stream (plaintext for encryption, ciphertext for decryption).</param>
    /// <param name="destinationStream">Writable output stream (ciphertext for encryption, plaintext for decryption).</param>
    /// <param name="cipher">Initialized AEAD cipher instance configured for encrypt or decrypt mode.</param>
    /// <returns>A task that completes when processing has finished.</returns>
    protected static async Task ProcessFileWithCipherAsync(
        FileStream sourceStream,
        FileStream destinationStream,
        IAeadCipher cipher
    )
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

    /// <summary>
    /// Validates that there is sufficient free disk space on the target drive to perform encryption, applying a safety margin.
    /// If disk information cannot be retrieved the method fails open and does not block the operation.
    /// </summary>
    /// <param name="sourceFilePath">Path to the source file whose size is used to estimate required space.</param>
    /// <param name="destinationFilePath">Destination file path used to determine the target drive.</param>
    /// <returns>A completed task once validation (or best-effort attempt) finishes.</returns>
    /// <exception cref="EncryptionInsufficientSpaceException">Thrown when the available space is determined to be insufficient.</exception>
    private static async Task ValidateDiskSpaceAsync(
        string sourceFilePath,
        string destinationFilePath
    )
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
                    long requiredSpace = (long)(sourceFileInfo.Length * 1.2) + 1024;

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
}
