namespace CloudZCrypt.Application.DataTransferObjects.Files;


public record FileEncryptionProcessStatus(
    int ProcessedFiles,
    int TotalFiles,
    long ProcessedBytes,
    long TotalBytes,
    TimeSpan Elapsed);