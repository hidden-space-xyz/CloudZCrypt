using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IFileProcessingOrchestrator
{
    Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    Task<Result<FileProcessingResult>> ExecuteAsync(
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken = default
    );
}

public sealed record FileProcessingOrchestratorRequest(
    string SourcePath,
    string DestinationPath,
    string Password,
    string ConfirmPassword,
    EncryptionAlgorithm EncryptionAlgorithm,
    KeyDerivationAlgorithm KeyDerivationAlgorithm,
    EncryptOperation Operation
);
