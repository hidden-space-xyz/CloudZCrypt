namespace CloudZCrypt.Application.DataTransferObjects.Files;

/// <summary>
/// Data Transfer Object for file processing status
/// Maps from Domain.ValueObjects.FileProcessingStatus
/// </summary>
public record FileProcessingStatus(
    int ProcessedFiles,
    int TotalFiles,
    long ProcessedBytes,
    long TotalBytes,
    TimeSpan Elapsed)
{
    /// <summary>
    /// Creates a DTO from the domain value object
    /// </summary>
    public static FileProcessingStatus FromDomain(Domain.ValueObjects.FileProcessing.FileProcessingStatus domainStatus)
        => new(
            domainStatus.ProcessedFiles,
            domainStatus.TotalFiles,
            domainStatus.ProcessedBytes,
            domainStatus.TotalBytes,
            domainStatus.Elapsed);

    /// <summary>
    /// Converts to domain value object
    /// </summary>
    public Domain.ValueObjects.FileProcessing.FileProcessingStatus ToDomain()
        => new(
            ProcessedFiles,
            TotalFiles,
            ProcessedBytes,
            TotalBytes,
            Elapsed);
}