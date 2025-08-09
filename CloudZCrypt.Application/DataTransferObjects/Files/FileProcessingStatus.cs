namespace CloudZCrypt.Application.DataTransferObjects.Files;


public record FileProcessingStatus(
    int ProcessedFiles,
    int TotalFiles,
    long ProcessedBytes,
    long TotalBytes,
    TimeSpan Elapsed);