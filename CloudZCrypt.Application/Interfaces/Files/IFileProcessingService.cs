using CloudZCrypt.Application.DataTransferObjects.FileProcessing;
using CloudZCrypt.Application.Interfaces.Encryption;

namespace CloudZCrypt.Application.Interfaces.Files;

public interface IFileProcessingService
{
    Task<FileProcessingResult> ProcessFilesAsync(
        FileProcessingRequest request,
        IEncryptionService encryptionService,
        IProgress<FileEncryptionProcessStatus>? progress = null,
        CancellationToken cancellationToken = default);
}