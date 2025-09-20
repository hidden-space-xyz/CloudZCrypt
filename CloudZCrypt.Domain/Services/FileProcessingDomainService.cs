using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Services;
internal class FileProcessingDomainService(IFileOperationsService fileOperations, ISystemStorageService systemStorage) : IFileProcessingDomainService
{
    private const long DefaultBatchSizeLimit = 100 * 1024 * 1024;
    private const int MaxFilesPerBatch = 1000;
    private const double DefaultQualityThreshold = 0.95;

    public async Task<(bool CanProceed, IEnumerable<string> ValidationErrors)> ValidateProcessingOperationAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            errors.Add("Source directory cannot be empty.");
        }
        else if (!fileOperations.DirectoryExists(sourceDirectory))
        {
            errors.Add($"Source directory does not exist: {sourceDirectory}");
        }

        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            errors.Add("Destination directory cannot be empty.");
        }

        if (!string.IsNullOrWhiteSpace(sourceDirectory) &&
            !string.IsNullOrWhiteSpace(destinationDirectory))
        {
            string sourceFull = Path.GetFullPath(sourceDirectory);
            string destinationFull = Path.GetFullPath(destinationDirectory);
            if (sourceFull.Equals(destinationFull, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Source and destination directories cannot be the same.");
            }
        }

        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            try
            {
                string destinationRoot = systemStorage.GetPathRoot(Path.GetFullPath(destinationDirectory)) ?? destinationDirectory;

                if (systemStorage.IsDriveReady(destinationRoot) && fileOperations.DirectoryExists(sourceDirectory))
                {
                    string[] sourceFiles = await fileOperations.GetFilesAsync(sourceDirectory, "*.*", cancellationToken);
                    long requiredSpace = sourceFiles.Sum(f =>
                    {
                        try
                        {
                            return fileOperations.GetFileSize(f);
                        }
                        catch
                        {
                            return 0;
                        }
                    });

                    long requiredSpaceWithBuffer = (long)(requiredSpace * 1.1);
                    long available = systemStorage.GetAvailableFreeSpace(destinationRoot);

                    if (available >= 0 && available < requiredSpaceWithBuffer)
                    {
                        errors.Add($"Insufficient disk space. Required: {FormatBytes(requiredSpaceWithBuffer)}, Available: {FormatBytes(available)}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Could not check disk space: {ex.Message}");
            }
        }

        await Task.CompletedTask;

        return (errors.Count == 0, errors);
    }

    public int CalculateOptimalBatchSize(IEnumerable<string> filePaths, long availableMemory)
    {
        List<long> lengths = filePaths
            .Select(path =>
            {
                try
                {
                    return fileOperations.GetFileSize(path);
                }
                catch
                {
                    return (long?)null;
                }
            })
            .Where(len => len.HasValue)
            .Select(len => len!.Value)
            .ToList();

        if (!lengths.Any())
            return 1;

        List<long> sorted = lengths.OrderByDescending(l => l).ToList();

        long totalSize = 0;
        int batchSize = 0;
        long memoryLimit = Math.Min(availableMemory, DefaultBatchSizeLimit);

        foreach (long fileSize in sorted)
        {
            if (batchSize >= MaxFilesPerBatch)
                break;

            long estimatedMemoryUsage = (long)(fileSize * 1.5);

            if (totalSize + estimatedMemoryUsage > memoryLimit)
                break;

            totalSize += estimatedMemoryUsage;
            batchSize++;
        }

        return Math.Max(1, batchSize);
    }

    public bool MeetsQualityThreshold(FileProcessingResult result, double minimumSuccessRate = DefaultQualityThreshold)
    {
        ArgumentNullException.ThrowIfNull(result);

        return minimumSuccessRate is < 0 or > 1
            ? throw new ArgumentOutOfRangeException(nameof(minimumSuccessRate), "Success rate must be between 0 and 1")
            : result.SuccessRate >= minimumSuccessRate;
    }

    public IEnumerable<string> AnalyzeResult(FileProcessingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        List<string> recommendations = [];

        if (result.ElapsedTime.TotalMinutes > 10 && result.TotalFiles > 100)
        {
            recommendations.Add("Consider processing files in smaller batches for large operations.");
        }

        if (result.BytesPerSecond < 1024 * 1024)
        {
            recommendations.Add("Processing speed is below optimal. Consider checking disk performance or reducing concurrent operations.");
        }

        if (result.HasErrors)
        {
            double errorRate = 1.0 - result.SuccessRate;

            if (errorRate > 0.1)
            {
                recommendations.Add($"High error rate detected ({errorRate:P1}). Review error messages and consider retrying failed files.");
            }

            if (result.FailedFiles > 0)
            {
                recommendations.Add($"{result.FailedFiles} files failed to process. Check file permissions and available disk space.");
            }
        }

        if (result.IsSuccess && result.ProcessedFiles > 0)
        {
            recommendations.Add($"Successfully processed {result.ProcessedFiles} files in {result.ElapsedTime:hh\\:mm\\:ss}.");

            if (result.FilesPerSecond > 1)
            {
                recommendations.Add($"Good processing rate: {result.FilesPerSecond:F1} files/second.");
            }
        }

        if (result.IsPartialSuccess)
        {
            recommendations.Add("Operation completed with some errors. Consider retrying failed files.");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Operation completed successfully with no specific recommendations.");
        }

        return recommendations;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}
