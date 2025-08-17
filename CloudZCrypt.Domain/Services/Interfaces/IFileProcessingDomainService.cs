using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Domain service interface for file processing operations
/// Encapsulates complex business logic following DDD principles
/// </summary>
public interface IFileProcessingDomainService
{
    /// <summary>
    /// Validates if a file processing operation can proceed
    /// </summary>
    Task<(bool CanProceed, IEnumerable<string> ValidationErrors)> ValidateProcessingOperationAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the optimal batch size for processing files
    /// </summary>
    int CalculateOptimalBatchSize(IEnumerable<string> filePaths, long availableMemory);

    /// <summary>
    /// Determines if the processing result meets quality thresholds
    /// </summary>
    bool MeetsQualityThreshold(FileProcessingResult result, double minimumSuccessRate = 0.95);

    /// <summary>
    /// Analyzes the processing result and provides recommendations
    /// </summary>
    IEnumerable<string> AnalyzeResult(FileProcessingResult result);
}