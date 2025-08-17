using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories.Interfaces;

/// <summary>
/// Factory interface for creating FileProcessingStatus instances
/// </summary>
public interface IFileProcessingStatusFactory
{
    /// <summary>
    /// Creates a file processing status for ongoing operations
    /// </summary>
    FileProcessingStatus CreateInProgress(
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes,
        TimeSpan elapsed);

    /// <summary>
    /// Creates an initial file processing status
    /// </summary>
    FileProcessingStatus CreateInitial(
        int totalFiles,
        long totalBytes);

    /// <summary>
    /// Creates a completed file processing status
    /// </summary>
    FileProcessingStatus CreateCompleted(
        int totalFiles,
        long totalBytes,
        TimeSpan elapsed);
}