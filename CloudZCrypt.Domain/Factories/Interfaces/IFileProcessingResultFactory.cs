using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories.Interfaces;

/// <summary>
/// Factory interface for creating FileProcessingResult instances
/// </summary>
public interface IFileProcessingResultFactory
{
    /// <summary>
    /// Creates a successful file processing result
    /// </summary>
    FileProcessingResult CreateSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles);

    /// <summary>
    /// Creates a failed file processing result
    /// </summary>
    FileProcessingResult CreateFailure(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors);

    /// <summary>
    /// Creates a partial success file processing result (some files failed)
    /// </summary>
    FileProcessingResult CreatePartialSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors);

    /// <summary>
    /// Creates an empty result for when no files are found
    /// </summary>
    FileProcessingResult CreateEmpty(
        TimeSpan elapsedTime,
        string reason = "No files found in the source directory.");

    /// <summary>
    /// Creates a cancelled operation result
    /// </summary>
    FileProcessingResult CreateCancelled(
        TimeSpan elapsedTime,
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes);
}