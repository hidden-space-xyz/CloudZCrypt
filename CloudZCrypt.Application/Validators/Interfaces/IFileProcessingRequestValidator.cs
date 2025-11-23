using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Application.Validators.Interfaces;

public interface IFileProcessingRequestValidator
{
    Task<IReadOnlyList<string>> AnalyzeErrorsAsync(
        FileCryptRequest request,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileCryptRequest request,
        CancellationToken cancellationToken = default
    );
}
