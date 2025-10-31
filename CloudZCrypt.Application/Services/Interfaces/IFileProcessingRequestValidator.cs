using CloudZCrypt.Application.ValueObjects;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IFileProcessingRequestValidator
{
    Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );
}
