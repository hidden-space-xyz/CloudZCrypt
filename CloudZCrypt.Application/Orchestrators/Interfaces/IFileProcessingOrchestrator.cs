using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Application.Orchestrators.Interfaces;

public interface IFileProcessingOrchestrator
{
    Task<Result<FileProcessingResult>> ExecuteAsync(
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> ValidateErrorsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> ValidateWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );
}
