using CloudZCrypt.Application.ValueObjects;
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
