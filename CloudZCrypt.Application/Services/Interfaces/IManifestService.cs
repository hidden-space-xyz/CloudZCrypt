using CloudZCrypt.Application.ValueObjects.Manifest;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileCrypt;

namespace CloudZCrypt.Application.Services.Interfaces;

public interface IManifestService
{
    Task<Dictionary<string, string>?> TryReadManifestAsync(
        string sourceRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileCryptRequest request,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<string>> TrySaveManifestAsync(
        IReadOnlyList<ManifestEntry> entries,
        string destinationRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileCryptRequest request,
        CancellationToken cancellationToken
    );
}
