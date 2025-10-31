using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IManifestService
{
    Task<Dictionary<string, string>?> TryReadMapAsync(
        string sourceRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<string>> WriteAsync(
        IReadOnlyList<NameMapEntry> entries,
        string destinationRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    );
}
