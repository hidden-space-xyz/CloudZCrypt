using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.Services.Interfaces
{
    public interface IFileEncryptionApplicationService
    {
        Task<Result<FileProcessingResult>> DecryptFilesAsync(string sourceDirectory, string destinationDirectory, string password, EncryptionAlgorithm encryptionAlgorithm, KeyDerivationAlgorithm keyDerivationAlgorithm, IProgress<FileProcessingStatus>? progress = null, CancellationToken cancellationToken = default);
        Task<Result<FileProcessingResult>> EncryptFilesAsync(string sourceDirectory, string destinationDirectory, string password, EncryptionAlgorithm encryptionAlgorithm, KeyDerivationAlgorithm keyDerivationAlgorithm, IProgress<FileProcessingStatus>? progress = null, CancellationToken cancellationToken = default);
    }
}