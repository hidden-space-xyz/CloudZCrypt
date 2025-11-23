using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.ValueObjects.FileCrypt;

namespace CloudZCrypt.Application.Orchestrators.Interfaces;

public interface IFileCryptOrchestrator
{
    Task<Result<FileCryptResult>> ExecuteAsync(
        FileCryptRequest request,
        IProgress<FileCryptStatus> progress,
        CancellationToken cancellationToken = default
    );
}
