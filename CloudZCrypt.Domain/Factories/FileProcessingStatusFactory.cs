using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories;

/// <summary>
/// Factory implementation for creating FileProcessingStatus instances
/// Following Domain-Driven Design principles
/// </summary>
internal class FileProcessingStatusFactory : IFileProcessingStatusFactory
{
    public FileProcessingStatus CreateInProgress(
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes,
        TimeSpan elapsed)
    {
        return new FileProcessingStatus(
            processedFiles: processedFiles,
            totalFiles: totalFiles,
            processedBytes: processedBytes,
            totalBytes: totalBytes,
            elapsed: elapsed);
    }

    public FileProcessingStatus CreateInitial(
        int totalFiles,
        long totalBytes)
    {
        return new FileProcessingStatus(
            processedFiles: 0,
            totalFiles: totalFiles,
            processedBytes: 0,
            totalBytes: totalBytes,
            elapsed: TimeSpan.Zero);
    }

    public FileProcessingStatus CreateCompleted(
        int totalFiles,
        long totalBytes,
        TimeSpan elapsed)
    {
        return new FileProcessingStatus(
            processedFiles: totalFiles,
            totalFiles: totalFiles,
            processedBytes: totalBytes,
            totalBytes: totalBytes,
            elapsed: elapsed);
    }
}