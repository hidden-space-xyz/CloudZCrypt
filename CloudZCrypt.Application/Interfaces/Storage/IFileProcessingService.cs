using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Entities;

namespace CloudZCrypt.Application.Interfaces.Storage;

public interface IFileProcessingService
{
    Task<FileProcessingResult> EncryptFilesAsync(
        FileProcessingRequest request,
        IProgress<FileProcessingStatus>? progress = null,
        CancellationToken cancellationToken = default);
}