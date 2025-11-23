using CloudZCrypt.Domain.ValueObjects.FileCrypt;

namespace CloudZCrypt.Application.Validators.Interfaces;

public interface IFileCryptRequestValidator
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
