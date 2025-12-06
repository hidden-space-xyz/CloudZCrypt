using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Exceptions;

public abstract class EncryptionException : Exception
{
    public EncryptionErrorCode Code { get; }

    protected EncryptionException(EncryptionErrorCode code, string? message = null)
        : base(message ?? code.ToString())
    {
        Code = code;
    }

    protected EncryptionException(
        EncryptionErrorCode code,
        string? message,
        Exception innerException
    )
        : base(message ?? code.ToString(), innerException)
    {
        Code = code;
    }
}

public class EncryptionAccessDeniedException(string filePath, Exception innerException)
    : EncryptionException(
        EncryptionErrorCode.AccessDenied,
        message: $"Access denied to file: {filePath}",
        innerException
    )
{ }

public class EncryptionFileNotFoundException(string filePath)
    : EncryptionException(
        EncryptionErrorCode.FileNotFound,
        message: $"File not found: {filePath}"
    )
{ }

public class EncryptionInsufficientSpaceException(string path)
    : EncryptionException(
        EncryptionErrorCode.InsufficientDiskSpace,
        message: $"Insufficient disk space at: {path}"
    )
{ }

public class EncryptionInvalidPasswordException()
    : EncryptionException(
        EncryptionErrorCode.InvalidPassword,
        message: "Invalid password or corrupted file. Please verify the password and file integrity."
    )
{ }

public class EncryptionCorruptedFileException(string filePath)
    : EncryptionException(
        EncryptionErrorCode.FileCorruption,
        message: $"The encrypted file appears to be corrupted or invalid: {filePath}"
    )
{ }

public class EncryptionKeyDerivationException(Exception innerException)
    : EncryptionException(
        EncryptionErrorCode.KeyDerivationFailed,
        message: "Failed to derive encryption key from password",
        innerException
    )
{ }

public class EncryptionCipherException(string operation, Exception innerException)
    : EncryptionException(
        EncryptionErrorCode.CipherOperationFailed,
        message: $"Cipher operation failed during {operation}",
        innerException
    )
{ }
