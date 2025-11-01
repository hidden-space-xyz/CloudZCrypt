using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Strategies.Interfaces;
using System.Text;
using System.Text.Json;

namespace CloudZCrypt.Application.Services;

internal sealed class ManifestService : IManifestService
{
    private static string AppFileExtension => ".czc";
    private static string ManifestFileName => "manifest" + AppFileExtension;

    public async Task<Dictionary<string, string>?> TryReadManifestAsync(
        string sourceRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            string encryptedManifestPath = Path.Combine(sourceRoot, ManifestFileName);
            if (!File.Exists(encryptedManifestPath))
            {
                return null;
            }

            string tempJsonPath = Path.Combine(
                Path.GetTempPath(),
                $"czc-manifest-{Guid.NewGuid():N}.json"
            );
            bool ok = await encryptionService.DecryptFileAsync(
                encryptedManifestPath,
                tempJsonPath,
                request.Password,
                request.KeyDerivationAlgorithm
            );
            if (!ok)
            {
                try
                {
                    if (File.Exists(tempJsonPath))
                        File.Delete(tempJsonPath);
                }
                catch { /* ignore */ }
                return null;
            }

            try
            {
                await using FileStream fs = File.OpenRead(tempJsonPath);
                List<NameMapEntry>? entries = await JsonSerializer.DeserializeAsync<
                    List<NameMapEntry>
                >(fs, cancellationToken: cancellationToken);
                Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
                if (entries is not null)
                {
                    foreach (NameMapEntry e in entries)
                    {
                        // Key is obfuscated relative path, value is original relative path
                        map[e.ObfuscatedRelativePath] = e.OriginalRelativePath;
                    }
                }
                return map;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempJsonPath))
                        File.Delete(tempJsonPath);
                }
                catch { /* ignore */ }
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> TrySaveManifestAsync(
        IReadOnlyList<NameMapEntry> entries,
        string destinationRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    )
    {
        List<string> errors = [];
        if (entries.Count == 0)
        {
            return errors;
        }

        try
        {
            byte[] manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entries));
            string encryptedManifestPath = Path.Combine(destinationRoot, ManifestFileName);
            bool manifestOk = await encryptionService.CreateEncryptedFileAsync(
                manifestBytes,
                encryptedManifestPath,
                request.Password,
                request.KeyDerivationAlgorithm
            );
            if (!manifestOk)
            {
                errors.Add($"Failed to create encrypted manifest at '{encryptedManifestPath}'.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to write or encrypt manifest: {ex.Message}");
        }

        return errors;
    }
}
