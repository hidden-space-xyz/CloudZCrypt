using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Domain.Factories.Interfaces;
public interface IFileProcessingStatusFactory
{
    FileProcessingStatus CreateInProgress(
        int processedFiles,
        int totalFiles,
        long processedBytes,
        long totalBytes,
        TimeSpan elapsed);
    FileProcessingStatus CreateInitial(
        int totalFiles,
        long totalBytes);
    FileProcessingStatus CreateCompleted(
        int totalFiles,
        long totalBytes,
        TimeSpan elapsed);
}
