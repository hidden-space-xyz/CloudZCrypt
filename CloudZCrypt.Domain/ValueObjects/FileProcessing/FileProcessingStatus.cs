namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

/// <summary>
/// Represents the current status of a batch file processing operation, including progress metrics
/// based on file count, byte count, and elapsed processing time.
/// </summary>
/// <remarks>
/// This immutable value object is intended to be created and updated by file processing workflows
/// in order to expose read-only progress information to callers (e.g., UI, logging, monitoring).
/// All percentage properties return a value in the range 0.0 to 1.0, or 0.0 when the denominator is zero.
/// </remarks>
public sealed record FileProcessingStatus
{
    /// <summary>
    /// Gets the number of files that have been successfully processed so far.
    /// </summary>
    public int ProcessedFiles { get; }

    /// <summary>
    /// Gets the total number of files scheduled to be processed in the operation.
    /// </summary>
    public int TotalFiles { get; }

    /// <summary>
    /// Gets the cumulative number of bytes that have been processed so far.
    /// </summary>
    public long ProcessedBytes { get; }

    /// <summary>
    /// Gets the total number of bytes expected to be processed once the operation completes.
    /// </summary>
    public long TotalBytes { get; }

    /// <summary>
    /// Gets the total elapsed time since the start of the processing operation.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileProcessingStatus"/> class with the specified progress metrics.
    /// </summary>
    /// <param name="processedFiles">The number of files already processed. Must be non-negative and not exceed <paramref name="totalFiles"/>.</param>
    /// <param name="totalFiles">The total number of files to process. Must be non-negative.</param>
    /// <param name="processedBytes">The number of bytes already processed. Must be non-negative and not exceed <paramref name="totalBytes"/>.</param>
    /// <param name="totalBytes">The total number of bytes expected to be processed. Must be non-negative.</param>
    /// <param name="elapsed">The total elapsed time since processing began. Must not be negative.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when any numeric argument is negative; when <paramref name="processedFiles"/> exceeds <paramref name="totalFiles"/>;
    /// or when <paramref name="processedBytes"/> exceeds <paramref name="totalBytes"/>.
    /// </exception>
    public FileProcessingStatus(
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes,
        TimeSpan elapsed
    )
    {
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

        if (processedBytes < 0)
        {
            throw new ArgumentException(
                "Processed bytes cannot be negative",
                nameof(processedBytes)
            );
        }

        if (totalBytes < 0)
        {
            throw new ArgumentException("Total bytes cannot be negative", nameof(totalBytes));
        }

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentException("Elapsed time cannot be negative", nameof(elapsed));
        }

        if (processedFiles > totalFiles)
        {
            throw new ArgumentException(
                "Processed files cannot exceed total files",
                nameof(processedFiles)
            );
        }

        if (processedBytes > totalBytes)
        {
            throw new ArgumentException(
                "Processed bytes cannot exceed total bytes",
                nameof(processedBytes)
            );
        }

        ProcessedFiles = processedFiles;
        TotalFiles = totalFiles;
        ProcessedBytes = processedBytes;
        TotalBytes = totalBytes;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Gets the progress of file processing as a fraction (0.0 to 1.0). Returns 0.0 when <see cref="TotalFiles"/> is zero.
    /// </summary>
    public double FileProgressPercentage =>
        TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Gets the progress of byte processing as a fraction (0.0 to 1.0). Returns 0.0 when <see cref="TotalBytes"/> is zero.
    /// </summary>
    public double ByteProgressPercentage =>
        TotalBytes == 0 ? 0.0 : (double)ProcessedBytes / TotalBytes;

    /// <summary>
    /// Gets an estimate of the remaining time to complete processing based on the average time per processed file, or null if insufficient data is available.
    /// </summary>
    /// <remarks>
    /// The estimate is calculated using the average elapsed time per processed file multiplied by the number of remaining files.
    /// Returns null when no files have yet been processed or when <see cref="Elapsed"/> is zero.
    /// </remarks>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the estimated remaining time, or null when indeterminate.
    /// </value>
    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (ProcessedFiles == 0 || Elapsed == TimeSpan.Zero)
            {
                return null;
            }

            double avgTimePerFile = Elapsed.TotalMilliseconds / ProcessedFiles;
            int remainingFiles = TotalFiles - ProcessedFiles;

            return TimeSpan.FromMilliseconds(avgTimePerFile * remainingFiles);
        }
    }

    /// <summary>
    /// Gets the average number of files processed per second. Returns 0 when <see cref="Elapsed"/> total seconds is zero.
    /// </summary>
    public double FilesPerSecond =>
        Elapsed.TotalSeconds == 0 ? 0 : ProcessedFiles / Elapsed.TotalSeconds;

    /// <summary>
    /// Gets the average number of bytes processed per second. Returns 0 when <see cref="Elapsed"/> total seconds is zero.
    /// </summary>
    public double BytesPerSecond =>
        Elapsed.TotalSeconds == 0 ? 0 : ProcessedBytes / Elapsed.TotalSeconds;
}
