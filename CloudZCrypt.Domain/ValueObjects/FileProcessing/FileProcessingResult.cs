namespace CloudZCrypt.Domain.ValueObjects.FileProcessing;

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

    public double CompletionPercentage =>
        TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;
    public bool IsCompleted => ProcessedFiles == TotalFiles;
    public bool HasErrors => Errors.Count > 0;
    public int FailedFiles => TotalFiles - ProcessedFiles;
    public double SuccessRate => TotalFiles == 0 ? 0.0 : (double)ProcessedFiles / TotalFiles;
    public bool IsPartialSuccess => ProcessedFiles > 0 && ProcessedFiles < TotalFiles;
    public double BytesPerSecond =>
        ElapsedTime.TotalSeconds > 0 ? TotalBytes / ElapsedTime.TotalSeconds : 0;
    public double FilesPerSecond =>
        ElapsedTime.TotalSeconds > 0 ? ProcessedFiles / ElapsedTime.TotalSeconds : 0;

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
