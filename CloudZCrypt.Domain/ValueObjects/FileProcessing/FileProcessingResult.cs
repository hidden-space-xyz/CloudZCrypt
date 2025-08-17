namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

/// <summary>
/// Domain value object representing the result of a file processing operation
/// Immutable and encapsulates all validation logic following DDD principles
/// 
/// Architectural Notes:
/// - This is a Domain Value Object (DDD) - immutable and side-effect free
/// - Contains domain logic for validation and computed properties
/// - Should be created through IFileProcessingResultFactory for consistency
/// - Used by Command Handlers (CQRS) and converted to DTOs at Application boundaries
/// - Contains rich domain behavior while maintaining immutability
/// </summary>
public sealed record FileProcessingResult
{
    public bool IsSuccess { get; }
    public TimeSpan ElapsedTime { get; }
    public long TotalBytes { get; }
    public int ProcessedFiles { get; }
    public int TotalFiles { get; }
    public IReadOnlyList<string> Errors { get; }

    public FileProcessingResult(
        bool isSuccess,
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors)
    {
        ValidateInputs(elapsedTime, totalBytes, processedFiles, totalFiles);

        IsSuccess = isSuccess;
        ElapsedTime = elapsedTime;
        TotalBytes = totalBytes;
        ProcessedFiles = processedFiles;
        TotalFiles = totalFiles;
        Errors = errors?.ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the completion percentage (0.0 to 1.0)
    /// </summary>
    public double CompletionPercentage => TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Gets whether the operation has completed
    /// </summary>
    public bool IsCompleted => ProcessedFiles == TotalFiles;

    /// <summary>
    /// Gets whether there are any errors
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets the number of failed files
    /// </summary>
    public int FailedFiles => TotalFiles - ProcessedFiles;

    /// <summary>
    /// Gets the success rate (0.0 to 1.0)
    /// </summary>
    public double SuccessRate => TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Indicates whether this was a partial success (some files processed, some failed)
    /// </summary>
    public bool IsPartialSuccess => ProcessedFiles > 0 && ProcessedFiles < TotalFiles;

    /// <summary>
    /// Gets the average processing speed in bytes per second
    /// </summary>
    public double BytesPerSecond => ElapsedTime.TotalSeconds > 0 ? TotalBytes / ElapsedTime.TotalSeconds : 0;

    /// <summary>
    /// Gets the average files per second processing rate
    /// </summary>
    public double FilesPerSecond => ElapsedTime.TotalSeconds > 0 ? ProcessedFiles / ElapsedTime.TotalSeconds : 0;

    private static void ValidateInputs(TimeSpan elapsedTime, long totalBytes, int processedFiles, int totalFiles)
    {
        if (elapsedTime < TimeSpan.Zero)
            throw new ArgumentException("Elapsed time cannot be negative", nameof(elapsedTime));

        if (totalBytes < 0)
            throw new ArgumentException("Total bytes cannot be negative", nameof(totalBytes));

        if (processedFiles < 0)
            throw new ArgumentException("Processed files cannot be negative", nameof(processedFiles));

        if (totalFiles < 0)
            throw new ArgumentException("Total files cannot be negative", nameof(totalFiles));

        if (processedFiles > totalFiles)
            throw new ArgumentException("Processed files cannot exceed total files", nameof(processedFiles));
    }
}