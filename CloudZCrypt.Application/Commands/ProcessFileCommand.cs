using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using MediatR;

namespace CloudZCrypt.Application.Commands;

public record ProcessFileCommand : IRequest<Result<FileProcessingResult>>
{
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
    public required string Password { get; init; }
    public required EncryptionAlgorithm EncryptionAlgorithm { get; init; }
    public required KeyDerivationAlgorithm KeyDerivationAlgorithm { get; init; }
    public EncryptOperation EncryptOperation { get; init; } = EncryptOperation.Encrypt;
    public IProgress<FileProcessingStatus>? Progress { get; init; }
}