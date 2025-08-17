using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Services;

/// <summary>
/// Domain service implementation for file processing operations
/// Contains complex business logic that doesn't belong to any single entity
/// Following Domain-Driven Design principles
/// </summary>
internal class FileProcessingDomainService : IFileProcessingDomainService
{
    private const long DefaultBatchSizeLimit = 100 * 1024 * 1024; // 100MB
    private const int MaxFilesPerBatch = 1000;
    private const double DefaultQualityThreshold = 0.95;

    public async Task<(bool CanProceed, IEnumerable<string> ValidationErrors)> ValidateProcessingOperationAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        List<string> errors = [];

        // Validate source directory
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            errors.Add("Source directory cannot be empty.");
        }
        else if (!Directory.Exists(sourceDirectory))
        {
            errors.Add($"Source directory does not exist: {sourceDirectory}");
        }

        // Validate destination directory
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            errors.Add("Destination directory cannot be empty.");
        }

        // Check if source and destination are the same
        if (!string.IsNullOrWhiteSpace(sourceDirectory) &&
            !string.IsNullOrWhiteSpace(destinationDirectory) &&
            Path.GetFullPath(sourceDirectory).Equals(Path.GetFullPath(destinationDirectory), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Source and destination directories cannot be the same.");
        }

        // Check available disk space
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            try
            {
                string destinationRoot = Path.GetPathRoot(Path.GetFullPath(destinationDirectory)) ?? destinationDirectory;
                DriveInfo driveInfo = new(destinationRoot);

                if (driveInfo.IsReady)
                {
                    // Estimate required space (source files + 10% buffer)
                    if (Directory.Exists(sourceDirectory))
                    {
                        string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
                        long requiredSpace = sourceFiles.Sum(f =>
                        {
                            try
                            {
                                return new FileInfo(f).Length;
                            }
                            catch
                            {
                                return 0; // Skip files we can't access
                            }
                        });

                        long requiredSpaceWithBuffer = (long)(requiredSpace * 1.1); // 10% buffer

                        if (driveInfo.AvailableFreeSpace < requiredSpaceWithBuffer)
                        {
                            errors.Add($"Insufficient disk space. Required: {FormatBytes(requiredSpaceWithBuffer)}, Available: {FormatBytes(driveInfo.AvailableFreeSpace)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Could not check disk space: {ex.Message}");
            }
        }

        await Task.CompletedTask; // For future async operations

        return (errors.Count == 0, errors);
    }

    public int CalculateOptimalBatchSize(IEnumerable<string> filePaths, long availableMemory)
    {
        List<FileInfo?> fileInfos = filePaths
            .Select(path =>
            {
                try
                {
                    return new FileInfo(path);
                }
                catch
                {
                    return null; // Skip files we can't access
                }
            })
            .Where(fi => fi != null)
            .ToList();

        if (!fileInfos.Any())
            return 1;

        // Sort by size to process larger files first
        List<FileInfo?> sortedFiles = fileInfos.OrderByDescending(fi => fi!.Length).ToList();

        long totalSize = 0;
        int batchSize = 0;
        long memoryLimit = Math.Min(availableMemory, DefaultBatchSizeLimit);

        foreach (FileInfo? fileInfo in sortedFiles)
        {
            if (batchSize >= MaxFilesPerBatch)
                break;

            long fileSize = fileInfo!.Length;

            // Estimate memory usage (file size + processing overhead)
            long estimatedMemoryUsage = (long)(fileSize * 1.5); // 50% overhead

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

        if (minimumSuccessRate is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(minimumSuccessRate), "Success rate must be between 0 and 1");

        return result.SuccessRate >= minimumSuccessRate;
    }

    public IEnumerable<string> AnalyzeResult(FileProcessingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        List<string> recommendations = [];

        // Performance analysis
        if (result.ElapsedTime.TotalMinutes > 10 && result.TotalFiles > 100)
        {
            recommendations.Add("Consider processing files in smaller batches for large operations.");
        }

        if (result.BytesPerSecond < 1024 * 1024) // Less than 1MB/s
        {
            recommendations.Add("Processing speed is below optimal. Consider checking disk performance or reducing concurrent operations.");
        }

        // Error analysis
        if (result.HasErrors)
        {
            double errorRate = 1.0 - result.SuccessRate;

            if (errorRate > 0.1) // More than 10% failure
            {
                recommendations.Add($"High error rate detected ({errorRate:P1}). Review error messages and consider retrying failed files.");
            }

            if (result.FailedFiles > 0)
            {
                recommendations.Add($"{result.FailedFiles} files failed to process. Check file permissions and available disk space.");
            }
        }

        // Success analysis
        if (result.IsSuccess && result.ProcessedFiles > 0)
        {
            recommendations.Add($"Successfully processed {result.ProcessedFiles} files in {result.ElapsedTime:hh\\:mm\\:ss}.");

            if (result.FilesPerSecond > 1)
            {
                recommendations.Add($"Good processing rate: {result.FilesPerSecond:F1} files/second.");
            }
        }

        // Partial success analysis
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