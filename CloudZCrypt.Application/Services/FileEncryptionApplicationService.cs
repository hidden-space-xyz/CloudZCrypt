using CloudZCrypt.Application.Commands;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using MediatR;

namespace CloudZCrypt.Application.Services;

/// <summary>
/// Application service that demonstrates CQRS pattern usage
/// </summary>
internal class FileEncryptionApplicationService(IMediator mediator) : IFileEncryptionApplicationService
{

    /// <summary>
    /// Encrypts files using CQRS command
    /// </summary>
    public async Task<Result<FileProcessingResult>> EncryptFilesAsync(
        string sourceDirectory,
        string destinationDirectory,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm,
        IProgress<FileProcessingStatus>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EncryptFilesCommand command = new()
        {
            SourceDirectory = sourceDirectory,
            DestinationDirectory = destinationDirectory,
            Password = password,
            EncryptionAlgorithm = encryptionAlgorithm,
            KeyDerivationAlgorithm = keyDerivationAlgorithm,
            EncryptOperation = EncryptOperation.Encrypt,
            Progress = progress,
            CancellationToken = cancellationToken
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Decrypts files using CQRS command
    /// </summary>
    public async Task<Result<FileProcessingResult>> DecryptFilesAsync(
        string sourceDirectory,
        string destinationDirectory,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm,
        IProgress<FileProcessingStatus>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EncryptFilesCommand command = new()
        {
            SourceDirectory = sourceDirectory,
            DestinationDirectory = destinationDirectory,
            Password = password,
            EncryptionAlgorithm = encryptionAlgorithm,
            KeyDerivationAlgorithm = keyDerivationAlgorithm,
            EncryptOperation = EncryptOperation.Decrypt,
            Progress = progress,
            CancellationToken = cancellationToken
        };

        return await mediator.Send(command, cancellationToken);
    }
}