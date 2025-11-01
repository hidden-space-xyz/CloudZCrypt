using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IManifestService
{
    Task<Dictionary<string, string>?> TryReadManifestAsync(
        string sourceRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<string>> TrySaveManifestAsync(
        IReadOnlyList<NameMapEntry> entries,
        string destinationRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    );
}
