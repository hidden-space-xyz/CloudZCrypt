namespace CloudZCrypt.Domain.Exceptions;

/// <summary>
/// Represents the abstract base type for all domain-specific encryption and decryption related exceptions.
/// </summary>
/// <remarks>
/// This base class standardizes exception handling within the encryption subsystem by providing a
/// common root type that can be caught to handle any encryption failure scenario. More specific
/// exception types derive from this class to express distinct failure categories (e.g., file system
/// errors, invalid passwords, cryptographic failures, or environmental constraints).
/// <para>
/// Typical usage:
/// <code>
/// try
/// {
///     encryptor.EncryptFile(inputPath, outputPath, password);
/// }
/// catch (EncryptionException ex)
/// {
///     // Centralized logging / user notification
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class EncryptionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionException"/> class with a human-readable message.
    /// </summary>
    /// <param name="message">A descriptive message explaining the encryption-related error. Should not be null or empty.</param>
    protected EncryptionException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionException"/> class with a specified error message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A descriptive message explaining the encryption-related error. Should not be null or empty.</param>
    /// <param name="innerException">The exception that caused the current exception, enabling exception chaining. May be null.</param>
    protected EncryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when access to the specified file is denied during an encryption or decryption operation.
/// </summary>
/// <remarks>
/// This typically wraps underlying <see cref="System.UnauthorizedAccessException"/> or <see cref="System.IO.IOException"/>
/// scenarios to provide a domain-specific context.
/// </remarks>
/// <param name="filePath">The full path of the file for which access was denied.</param>
/// <param name="innerException">The underlying exception that triggered this access denial condition.</param>
public class EncryptionAccessDeniedException(string filePath, Exception innerException)
    : EncryptionException($"Access denied to file: {filePath}", innerException)
{ }

/// <summary>
/// Thrown when the specified file cannot be located for an encryption or decryption operation.
/// </summary>
/// <remarks>
/// This exception is raised in scenarios where the caller provided a non-existent path or the file was removed
/// between validation and processing. Consider verifying file existence before initiating the operation.
/// </remarks>
/// <param name="filePath">The full path of the file that was not found.</param>
public class EncryptionFileNotFoundException(string filePath)
    : EncryptionException($"File not found: {filePath}")
{ }

/// <summary>
/// Thrown when there is insufficient disk space to complete an encryption or decryption operation.
/// </summary>
/// <remarks>
/// This is typically detected prior to writing large output streams or temporary working files.
/// Callers may prompt the user to free space and retry.
/// </remarks>
/// <param name="path">The path (directory or drive) associated with the insufficient storage condition.</param>
public class EncryptionInsufficientSpaceException(string path)
    : EncryptionException($"Insufficient disk space at: {path}")
{ }

/// <summary>
/// Thrown when a decryption operation fails due to an invalid password or when the encrypted data integrity is compromised.
/// </summary>
/// <remarks>
/// Distinguishing between an incorrect password and a corrupted file may not be possible without leaking
/// side-channel information. The message intentionally combines both possibilities.
/// </remarks>
public class EncryptionInvalidPasswordException : EncryptionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionInvalidPasswordException"/> class with a standardized message.
    /// </summary>
    public EncryptionInvalidPasswordException()
        : base("Invalid password or corrupted file. Please verify the password and file integrity.")
    { }
}

/// <summary>
/// Thrown when an encrypted file is detected to be corrupted, malformed, or incompatible with the expected format.
/// </summary>
/// <remarks>
/// This may indicate tampering, partial writes, truncation, or an unsupported file revision/version.
/// </remarks>
/// <param name="filePath">The full path of the corrupted or invalid encrypted file.</param>
public class EncryptionCorruptedFileException(string filePath)
    : EncryptionException($"The encrypted file appears to be corrupted or invalid: {filePath}")
{ }

/// <summary>
/// Thrown when cryptographic key derivation from a password fails unexpectedly.
/// </summary>
/// <remarks>
/// This usually wraps lower-level exceptions originating from KDF implementations (e.g., PBKDF2, Argon2).
/// </remarks>
/// <param name="innerException">The underlying exception that caused the key derivation failure.</param>
public class EncryptionKeyDerivationException(Exception innerException)
    : EncryptionException("Failed to derive encryption key from password", innerException)
{ }

/// <summary>
/// Thrown when a cipher (encryption or decryption) operation fails during the specified phase.
/// </summary>
/// <remarks>
/// The <c>operation</c> parameter should describe the failing stage (e.g., "encryption finalization", "decryption block read").
/// </remarks>
/// <param name="operation">A short description of the cipher operation phase that failed.</param>
/// <param name="innerException">The underlying cryptographic or I/O exception that triggered this failure.</param>
public class EncryptionCipherException(string operation, Exception innerException)
    : EncryptionException($"Cipher operation failed during {operation}", innerException)
{ }
