using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects.FileCrypt;

public sealed record FileCryptResult
{
    public bool IsSuccess { get; }
    public TimeSpan ElapsedTime { get; }
    public long TotalBytes { get; }
    public int ProcessedFiles { get; }
    public int TotalFiles { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }

    public FileCryptResult(
        bool isSuccess,
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string>? errors = null,
        IEnumerable<string>? warnings = null
    )
    {
        ValidateInputs(elapsedTime, totalBytes, processedFiles, totalFiles);

        IsSuccess = isSuccess;
        ElapsedTime = elapsedTime;
        TotalBytes = totalBytes;
        ProcessedFiles = processedFiles;
        TotalFiles = totalFiles;
        Errors = errors?.ToArray() ?? Array.Empty<string>();
        Warnings = warnings?.ToArray() ?? Array.Empty<string>();
    }

    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
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
            throw new Exceptions.ValidationException(
                ValidationErrorCode.ElapsedTimeNegative,
                "Elapsed time cannot be negative",
                nameof(elapsedTime)
            );
        }

        if (totalBytes < 0)
        {
            throw new Exceptions.ValidationException(
                ValidationErrorCode.TotalBytesNegative,
                "Total bytes cannot be negative",
                nameof(totalBytes)
            );
        }

        if (processedFiles < 0)
        {
            throw new Exceptions.ValidationException(
                ValidationErrorCode.ProcessedFilesNegative,
                "Processed files cannot be negative",
                nameof(processedFiles)
            );
        }

        if (totalFiles < 0)
        {
            throw new Exceptions.ValidationException(
                ValidationErrorCode.TotalFilesNegative,
                "Total files cannot be negative",
                nameof(totalFiles)
            );
        }

        if (processedFiles > totalFiles)
        {
            throw new Exceptions.ValidationException(
                ValidationErrorCode.ProcessedFilesExceedTotalFiles,
                "Processed files cannot exceed total files",
                nameof(processedFiles)
            );
        }
    }
}
