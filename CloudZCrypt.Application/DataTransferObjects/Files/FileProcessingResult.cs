namespace CloudZCrypt.Application.DataTransferObjects.Files;
public record FileProcessingResult(
    bool IsSuccess,
    TimeSpan ElapsedTime,
    long TotalBytes,
    int ProcessedFiles,
    int TotalFiles,
    IReadOnlyList<string> Errors)
{
    public static FileProcessingResult FromDomain(Domain.ValueObjects.FileProcessing.FileProcessingResult domainResult)
        => new(
            domainResult.IsSuccess,
            domainResult.ElapsedTime,
            domainResult.TotalBytes,
            domainResult.ProcessedFiles,
            domainResult.TotalFiles,
            domainResult.Errors);
    public Domain.ValueObjects.FileProcessing.FileProcessingResult ToDomain()
        => new(
            IsSuccess,
            ElapsedTime,
            TotalBytes,
            ProcessedFiles,
            TotalFiles,
            Errors);
}
