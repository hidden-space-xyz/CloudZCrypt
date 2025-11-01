using CloudZCrypt.Application.ValueObjects;

namespace CloudZCrypt.Application.Validators.Interfaces;

public interface IFileProcessingRequestValidator
{
    Task<IReadOnlyList<string>> AnalyzeErrorsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );
}
