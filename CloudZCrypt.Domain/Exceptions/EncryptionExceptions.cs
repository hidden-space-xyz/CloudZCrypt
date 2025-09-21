namespace CloudZCrypt.Domain.Exceptions;

/// <summary>
/// Base exception for encryption-related errors
/// </summary>
public abstract class EncryptionException : Exception
{
    protected EncryptionException(string message)
        : base(message) { }

    protected EncryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when file access is denied during encryption/decryption
/// </summary>
public class EncryptionAccessDeniedException(string filePath, Exception innerException)
    : EncryptionException($"Access denied to file: {filePath}", innerException)
{ }

/// <summary>
/// Exception thrown when a file is not found during encryption/decryption
/// </summary>
public class EncryptionFileNotFoundException(string filePath)
    : EncryptionException($"File not found: {filePath}")
{ }

/// <summary>
/// Exception thrown when there's insufficient disk space for the operation
/// </summary>
public class EncryptionInsufficientSpaceException(string path)
    : EncryptionException($"Insufficient disk space at: {path}")
{ }

/// <summary>
/// Exception thrown when the password is incorrect for decryption
/// </summary>
public class EncryptionInvalidPasswordException : EncryptionException
{
    public EncryptionInvalidPasswordException()
        : base("Invalid password or corrupted file. Please verify the password and file integrity.")
    { }
}

/// <summary>
/// Exception thrown when the encrypted file is corrupted or invalid
/// </summary>
public class EncryptionCorruptedFileException(string filePath)
    : EncryptionException($"The encrypted file appears to be corrupted or invalid: {filePath}")
{ }

/// <summary>
/// Exception thrown when key derivation fails
/// </summary>
public class EncryptionKeyDerivationException(Exception innerException)
    : EncryptionException("Failed to derive encryption key from password", innerException)
{ }

/// <summary>
/// Exception thrown during cipher operations
/// </summary>
public class EncryptionCipherException(string operation, Exception innerException)
    : EncryptionException($"Cipher operation failed during {operation}", innerException)
{ }
