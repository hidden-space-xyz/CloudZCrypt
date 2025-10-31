using CloudZCrypt.Application.ValueObjects;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IFileProcessingWarningAnalyzer
{
    Task<IReadOnlyList<string>> AnalyzeAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );
}
