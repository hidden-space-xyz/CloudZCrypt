namespace CloudZCrypt.Application.DataTransferObjects.Files;
public record FileProcessingStatus(
    int ProcessedFiles,
    int TotalFiles,
    long ProcessedBytes,
    long TotalBytes,
    TimeSpan Elapsed)
{
    public static FileProcessingStatus FromDomain(Domain.ValueObjects.FileProcessing.FileProcessingStatus domainStatus)
        => new(
            domainStatus.ProcessedFiles,
            domainStatus.TotalFiles,
            domainStatus.ProcessedBytes,
            domainStatus.TotalBytes,
            domainStatus.Elapsed);
    public Domain.ValueObjects.FileProcessing.FileProcessingStatus ToDomain()
        => new(
            ProcessedFiles,
            TotalFiles,
            ProcessedBytes,
            TotalBytes,
            Elapsed);
}
