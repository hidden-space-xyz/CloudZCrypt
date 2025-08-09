namespace CloudZCrypt.Application.DataTransferObjects.FileProcessing;


public record FileEncryptionProcessStatus(
    int ProcessedFiles,
    int TotalFiles,
    long ProcessedBytes,
    long TotalBytes,
    TimeSpan Elapsed);