namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

/// <summary>
/// Domain value object representing the status of an ongoing file processing operation
/// </summary>
public sealed record FileProcessingStatus
{
    public int ProcessedFiles { get; }
    public int TotalFiles { get; }
    public long ProcessedBytes { get; }
    public long TotalBytes { get; }
    public TimeSpan Elapsed { get; }

    public FileProcessingStatus(
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes,
        TimeSpan elapsed)
    {
        if (processedFiles < 0)
            throw new ArgumentException("Processed files cannot be negative", nameof(processedFiles));

        if (totalFiles < 0)
            throw new ArgumentException("Total files cannot be negative", nameof(totalFiles));

        if (processedBytes < 0)
            throw new ArgumentException("Processed bytes cannot be negative", nameof(processedBytes));

        if (totalBytes < 0)
            throw new ArgumentException("Total bytes cannot be negative", nameof(totalBytes));

        if (elapsed < TimeSpan.Zero)
            throw new ArgumentException("Elapsed time cannot be negative", nameof(elapsed));

        if (processedFiles > totalFiles)
            throw new ArgumentException("Processed files cannot exceed total files", nameof(processedFiles));

        if (processedBytes > totalBytes)
            throw new ArgumentException("Processed bytes cannot exceed total bytes", nameof(processedBytes));

        ProcessedFiles = processedFiles;
        TotalFiles = totalFiles;
        ProcessedBytes = processedBytes;
        TotalBytes = totalBytes;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Gets the file processing percentage (0.0 to 1.0)
    /// </summary>
    public double FileProgressPercentage => TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;

    /// <summary>
    /// Gets the byte processing percentage (0.0 to 1.0)
    /// </summary>
    public double ByteProgressPercentage => TotalBytes == 0 ? 0.0 : (double)ProcessedBytes / TotalBytes;

    /// <summary>
    /// Gets the estimated time remaining based on current progress
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (ProcessedFiles == 0 || Elapsed == TimeSpan.Zero)
                return null;

            double avgTimePerFile = Elapsed.TotalMilliseconds / ProcessedFiles;
            int remainingFiles = TotalFiles - ProcessedFiles;

            return TimeSpan.FromMilliseconds(avgTimePerFile * remainingFiles);
        }
    }

    /// <summary>
    /// Gets the processing speed in files per second
    /// </summary>
    public double FilesPerSecond => Elapsed.TotalSeconds == 0 ? 0 : ProcessedFiles / Elapsed.TotalSeconds;

    /// <summary>
    /// Gets the processing speed in bytes per second
    /// </summary>
    public double BytesPerSecond => Elapsed.TotalSeconds == 0 ? 0 : ProcessedBytes / Elapsed.TotalSeconds;
}