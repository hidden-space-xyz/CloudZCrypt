using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Services.Interfaces;
public interface IFileProcessingDomainService
{
    Task<(bool CanProceed, IEnumerable<string> ValidationErrors)> ValidateProcessingOperationAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken = default);
    int CalculateOptimalBatchSize(IEnumerable<string> filePaths, long availableMemory);
    bool MeetsQualityThreshold(FileProcessingResult result, double minimumSuccessRate = 0.95);
    IEnumerable<string> AnalyzeResult(FileProcessingResult result);
}
