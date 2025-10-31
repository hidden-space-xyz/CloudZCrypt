namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

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
}
