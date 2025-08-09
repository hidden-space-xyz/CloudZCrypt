namespace CloudZCrypt.Application.DataTransferObjects.FileProcessing;

public record FileProcessingResult(
    bool IsSuccess,
    TimeSpan ElapsedTime,
    long TotalBytes,
    int ProcessedFiles,
    int TotalFiles,
    List<string> Errors);