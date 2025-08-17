using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories;

/// <summary>
/// Factory implementation for creating FileProcessingResult instances
/// Following Domain-Driven Design principles
/// </summary>
internal class FileProcessingResultFactory : IFileProcessingResultFactory
{
    public FileProcessingResult CreateSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles)
    {
        return new FileProcessingResult(
            isSuccess: true,
            elapsedTime: elapsedTime,
            totalBytes: totalBytes,
            processedFiles: processedFiles,
            totalFiles: totalFiles,
            errors: Array.Empty<string>());
    }

    public FileProcessingResult CreateFailure(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors)
    {
        return new FileProcessingResult(
            isSuccess: false,
            elapsedTime: elapsedTime,
            totalBytes: totalBytes,
            processedFiles: processedFiles,
            totalFiles: totalFiles,
            errors: errors);
    }

    public FileProcessingResult CreatePartialSuccess(
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors)
    {
        // Partial success is when some files were processed but others failed
        bool hasProcessedFiles = processedFiles > 0;

        return new FileProcessingResult(
            isSuccess: hasProcessedFiles, // Success if at least some files were processed
            elapsedTime: elapsedTime,
            totalBytes: totalBytes,
            processedFiles: processedFiles,
            totalFiles: totalFiles,
            errors: errors);
    }

    public FileProcessingResult CreateEmpty(
        TimeSpan elapsedTime,
        string reason = "No files found in the source directory.")
    {
        return new FileProcessingResult(
            isSuccess: false,
            elapsedTime: elapsedTime,
            totalBytes: 0,
            processedFiles: 0,
            totalFiles: 0,
            errors: [reason]);
    }

    public FileProcessingResult CreateCancelled(
        TimeSpan elapsedTime,
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes)
    {
        return new FileProcessingResult(
            isSuccess: false,
            elapsedTime: elapsedTime,
            totalBytes: totalBytes,
            processedFiles: processedFiles,
            totalFiles: totalFiles,
            errors: [$"Operation was cancelled. Processed {processedFiles} of {totalFiles} files."]);
    }
}