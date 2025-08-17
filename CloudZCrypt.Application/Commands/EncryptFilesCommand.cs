using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.Commands;

/// <summary>
/// Command to encrypt files
/// Following CQRS principles, contains only the data needed for the operation
/// </summary>
public record EncryptFilesCommand : ICommand<Result<FileProcessingResult>>
{
    public required string SourceDirectory { get; init; }
    public required string DestinationDirectory { get; init; }
    public required string Password { get; init; }
    public required EncryptionAlgorithm EncryptionAlgorithm { get; init; }
    public required KeyDerivationAlgorithm KeyDerivationAlgorithm { get; init; }
    public EncryptOperation EncryptOperation { get; init; } = EncryptOperation.Encrypt;
    public IProgress<FileProcessingStatus>? Progress { get; init; }
}