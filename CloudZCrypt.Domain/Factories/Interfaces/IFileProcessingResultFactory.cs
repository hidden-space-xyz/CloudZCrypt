using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories.Interfaces;
public interface IFileProcessingResultFactory
{
    FileProcessingResult CreateSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles);
    FileProcessingResult CreateFailure(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors);
    FileProcessingResult CreatePartialSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors);
    FileProcessingResult CreateEmpty(
        TimeSpan elapsedTime,
        string reason = "No files found in the source directory.");
    FileProcessingResult CreateCancelled(
        TimeSpan elapsedTime,
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes);
}
