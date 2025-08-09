namespace CloudZCrypt.Application.DataTransferObjects.Files;

public record FileProcessingResult(
    bool IsSuccess,
    TimeSpan ElapsedTime,
    long TotalBytes,
    int ProcessedFiles,
    int TotalFiles,
    List<string> Errors);