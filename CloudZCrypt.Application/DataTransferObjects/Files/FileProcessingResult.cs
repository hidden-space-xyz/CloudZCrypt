namespace CloudZCrypt.Application.DataTransferObjects.Files;

/// <summary>
/// Data Transfer Object for file processing results
/// Maps from Domain.ValueObjects.FileProcessingResult
/// </summary>
public record FileProcessingResult(
    bool IsSuccess,
    TimeSpan ElapsedTime,
    long TotalBytes,
    int ProcessedFiles,
    int TotalFiles,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Creates a DTO from the domain value object
    /// </summary>
    public static FileProcessingResult FromDomain(Domain.ValueObjects.FileProcessing.FileProcessingResult domainResult)
        => new(
            domainResult.IsSuccess,
            domainResult.ElapsedTime,
            domainResult.TotalBytes,
            domainResult.ProcessedFiles,
            domainResult.TotalFiles,
            domainResult.Errors);

    /// <summary>
    /// Converts to domain value object
    /// </summary>
    public Domain.ValueObjects.FileProcessing.FileProcessingResult ToDomain()
        => new(
            IsSuccess,
            ElapsedTime,
            TotalBytes,
            ProcessedFiles,
            TotalFiles,
            Errors);
}