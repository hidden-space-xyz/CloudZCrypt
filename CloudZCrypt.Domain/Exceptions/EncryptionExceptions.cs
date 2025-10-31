namespace CloudZCrypt.Domain.Exceptions;

public abstract class EncryptionException : Exception
{
    protected EncryptionException(string message)
        : base(message) { }

    protected EncryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class EncryptionAccessDeniedException(string filePath, Exception innerException)
    : EncryptionException($"Access denied to file: {filePath}", innerException)
{ }

public class EncryptionFileNotFoundException(string filePath)
    : EncryptionException($"File not found: {filePath}")
{ }

public class EncryptionInsufficientSpaceException(string path)
    : EncryptionException($"Insufficient disk space at: {path}")
{ }

public class EncryptionInvalidPasswordException()
    : EncryptionException(
        "Invalid password or corrupted file. Please verify the password and file integrity."
    )
{ }

public class EncryptionCorruptedFileException(string filePath)
    : EncryptionException($"The encrypted file appears to be corrupted or invalid: {filePath}")
{ }

public class EncryptionKeyDerivationException(Exception innerException)
    : EncryptionException("Failed to derive encryption key from password", innerException)
{ }

public class EncryptionCipherException(string operation, Exception innerException)
    : EncryptionException($"Cipher operation failed during {operation}", innerException)
{ }
