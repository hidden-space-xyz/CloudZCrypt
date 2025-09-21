namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

/// <summary>
/// Represents the outcome and associated metrics of a batch file processing operation, including
/// success status, performance statistics, and any errors encountered during execution.
/// </summary>
/// <remarks>
/// This value object aggregates operational metadata such as elapsed processing time, total bytes
/// processed, file counts, throughput metrics, and error details. It is immutable and intended for
/// reporting, auditing, telemetry, or downstream decision logic.
/// <para>
/// The <see cref="IsSuccess"/> property reflects the overall operation status as determined by the
/// caller, while <see cref="HasErrors"/> and <see cref="IsPartialSuccess"/> provide finer-grained
/// diagnostic context.
/// </para>
/// </remarks>
public sealed record FileProcessingResult
{
    /// <summary>
    /// Gets a value indicating whether the overall file processing operation was reported as successful.
    /// </summary>
    /// <remarks>
    /// This flag does not imply the absence of errors if partial processing semantics are allowed.
    /// Check <see cref="HasErrors"/> or <see cref="IsPartialSuccess"/> for additional insight.
    /// </remarks>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the total elapsed wall-clock time consumed by the file processing operation.
    /// </summary>
    public TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the cumulative number of bytes successfully processed across all files.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the number of files that were successfully processed.
    /// </summary>
    public int ProcessedFiles { get; }

    /// <summary>
    /// Gets the total number of files that were scheduled or intended for processing.
    /// </summary>
    public int TotalFiles { get; }

    /// <summary>
    /// Gets a read-only collection of error messages captured during processing.
    /// </summary>
    /// <remarks>
    /// This list may contain multiple entries per file if layered operations (e.g., read, transform, persist) failed independently.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessingResult"/> record with the specified metrics and outcome details.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the overall operation is considered successful.</param>
    /// <param name="elapsedTime">The total elapsed time of the operation. Must be non-negative.</param>
    /// <param name="totalBytes">The cumulative number of processed bytes. Must be greater than or equal to zero.</param>
    /// <param name="processedFiles">The number of files successfully processed. Must be between zero and <paramref name="totalFiles"/> inclusive.</param>
    /// <param name="totalFiles">The total number of files targeted for processing. Must be greater than or equal to zero.</param>
    /// <param name="errors">A sequence of error messages encountered; may be <c>null</c> for no errors.</param>
    /// <exception cref="ArgumentException">Thrown when any numeric argument violates its documented constraints.</exception>
    public FileProcessingResult(
        bool isSuccess,
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors
    )
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
    /// Gets the completion percentage expressed as a fractional value between 0.0 and 1.0.
    /// </summary>
    public double CompletionPercentage =>
        TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Gets a value indicating whether all scheduled files have been processed (successfully or otherwise).
    /// </summary>
    public bool IsCompleted => ProcessedFiles == TotalFiles;

    /// <summary>
    /// Gets a value indicating whether any errors were recorded during processing.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets the number of files that were not successfully processed.
    /// </summary>
    public int FailedFiles => TotalFiles - ProcessedFiles;

    /// <summary>
    /// Gets the success rate expressed as a fractional value between 0.0 and 1.0 based on processed versus total files.
    /// </summary>
    public double SuccessRate => TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Gets a value indicating whether the operation achieved a partial (non-zero, non-complete) result.
    /// </summary>
    public bool IsPartialSuccess => ProcessedFiles > 0 && ProcessedFiles < TotalFiles;

    /// <summary>
    /// Gets the average number of bytes processed per second. Returns 0 if the elapsed time is zero.
    /// </summary>
    public double BytesPerSecond =>
        ElapsedTime.TotalSeconds > 0 ? TotalBytes / ElapsedTime.TotalSeconds : 0;

    /// <summary>
    /// Gets the average number of files processed per second. Returns 0 if the elapsed time is zero.
    /// </summary>
    public double FilesPerSecond =>
        ElapsedTime.TotalSeconds > 0 ? ProcessedFiles / ElapsedTime.TotalSeconds : 0;

    /// <summary>
    /// Validates constructor input arguments to ensure logical and range correctness.
    /// </summary>
    /// <param name="elapsedTime">The elapsed processing duration; must not be negative.</param>
    /// <param name="totalBytes">The total bytes processed; must be greater than or equal to zero.</param>
    /// <param name="processedFiles">The number of processed files; must be within the range [0, <paramref name="totalFiles"/>].</param>
    /// <param name="totalFiles">The total number of target files; must be greater than or equal to zero.</param>
    /// <exception cref="ArgumentException">Thrown when any argument is negative or when <paramref name="processedFiles"/> exceeds <paramref name="totalFiles"/>.</exception>
    private static void ValidateInputs(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles
    )
    {
        if (elapsedTime < TimeSpan.Zero)
        {
            throw new ArgumentException("Elapsed time cannot be negative", nameof(elapsedTime));
        }

        if (totalBytes < 0)
        {
            throw new ArgumentException("Total bytes cannot be negative", nameof(totalBytes));
        }

        if (processedFiles < 0)
        {
            throw new ArgumentException(
                "Processed files cannot be negative",
                nameof(processedFiles)
            );
        }

        if (totalFiles < 0)
        {
            throw new ArgumentException("Total files cannot be negative", nameof(totalFiles));
        }

        if (processedFiles > totalFiles)
        {
            throw new ArgumentException(
                "Processed files cannot exceed total files",
                nameof(processedFiles)
            );
        }
    }
}
