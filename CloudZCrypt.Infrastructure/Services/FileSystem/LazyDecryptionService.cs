using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using System.Collections.Concurrent;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

public class LazyDecryptionService : ILazyDecryptionService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
    private readonly ConcurrentDictionary<string, DateTime> _decryptedFileTimestamps = new();
    private readonly ConcurrentDictionary<string, FileDecryptionInfo> _fileDecryptionCache = new();

    public async Task CreateVaultStructureAsync(
        string encryptedDirectoryPath,
        string mountDirectoryPath,
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            // Get all encrypted files
            string[] encryptedFiles = Directory.GetFiles(encryptedDirectoryPath, "*.encrypted", SearchOption.AllDirectories);

            // Process files in parallel for better performance
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            
            await Parallel.ForEachAsync(encryptedFiles, parallelOptions, async (encryptedFile, cancellationToken) =>
            {
                try
                {
                    // Create the directory structure
                    string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedFile);
                    string decryptedFileName = relativePath.Replace(".encrypted", "");
                    string decryptedFilePath = Path.Combine(mountDirectoryPath, decryptedFileName);

                    // Ensure directory exists
                    string? directory = Path.GetDirectoryName(decryptedFilePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        lock (_fileLocks) // Ensure thread-safe directory creation
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }

                    // Create enhanced placeholder file with metadata
                    await CreateEnhancedPlaceholderFileAsync(decryptedFilePath, encryptedFile);
                    
                    // Cache file information for quick lookup
                    var encryptedFileInfo = new FileInfo(encryptedFile);
                    _fileDecryptionCache[decryptedFilePath] = new FileDecryptionInfo
                    {
                        EncryptedPath = encryptedFile,
                        OriginalSize = encryptedFileInfo.Length,
                        LastModified = encryptedFileInfo.LastWriteTime,
                        IsDecrypted = false
                    };
                }
                catch (Exception ex)
                {
                    // Log error but continue with other files
                }
            });

            // Create directory structure for any subdirectories
            string[] encryptedDirs = Directory.GetDirectories(encryptedDirectoryPath, "*", SearchOption.AllDirectories);
            foreach (string encryptedDir in encryptedDirs)
            {
                string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedDir);
                string decryptedDirPath = Path.Combine(mountDirectoryPath, relativePath);
                Directory.CreateDirectory(decryptedDirPath);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw to maintain compatibility
        }
    }

    public async Task<bool> DecryptFileOnDemandAsync(
        string encryptedFilePath,
        string decryptedFilePath,
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        // Use file-specific locking to prevent concurrent decryption of the same file
        var fileLock = _fileLocks.GetOrAdd(decryptedFilePath, _ => new SemaphoreSlim(1, 1));
        
        try
        {
            await fileLock.WaitAsync();
            
            // Check if file was already decrypted by another thread
            if (await IsFileFullyDecryptedAsync(decryptedFilePath))
            {
                return true;
            }

            // Check if encrypted file has been modified since last decryption
            if (_fileDecryptionCache.TryGetValue(decryptedFilePath, out var cachedInfo))
            {
                var encryptedFileInfo = new FileInfo(encryptedFilePath);
                if (encryptedFileInfo.LastWriteTime > cachedInfo.LastModified)
                {
                    // Encrypted file was modified, need to re-decrypt
                    cachedInfo.LastModified = encryptedFileInfo.LastWriteTime;
                    cachedInfo.IsDecrypted = false;
                }
                else if (cachedInfo.IsDecrypted)
                {
                    // File is already decrypted and up to date
                    return true;
                }
            }

            // Perform the actual decryption
            bool success = await encryptionService.DecryptFileAsync(encryptedFilePath, decryptedFilePath, password, keyDerivationAlgorithm);
            
            if (success)
            {
                // Update cache and timestamp
                _decryptedFileTimestamps[decryptedFilePath] = DateTime.UtcNow;
                if (_fileDecryptionCache.TryGetValue(decryptedFilePath, out var info))
                {
                    info.IsDecrypted = true;
                }
            }
            
            return success;
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<bool> RequiresDecryptionAsync(
        string encryptedFilePath,
        string decryptedFilePath)
    {
        try
        {
            // Check if encrypted file exists
            if (!File.Exists(encryptedFilePath))
                return false;

            // Check if decrypted file exists
            if (!File.Exists(decryptedFilePath))
                return true;

            // Quick cache lookup
            if (_fileDecryptionCache.TryGetValue(decryptedFilePath, out var cachedInfo))
            {
                if (cachedInfo.IsDecrypted)
                {
                    // Check if encrypted file was modified
                    var encryptedFileInfo = new FileInfo(encryptedFilePath);
                    return encryptedFileInfo.LastWriteTime > cachedInfo.LastModified;
                }
                return true;
            }

            // Check if the file is just a placeholder (very small size or contains placeholder marker)
            var fileInfo = new FileInfo(decryptedFilePath);
            if (fileInfo.Length <= 4) // Placeholder files are typically very small
            {
                // Check if it contains our placeholder marker
                byte[] content = await File.ReadAllBytesAsync(decryptedFilePath);
                if (content.Length == 1 && content[0] == 0x00)
                    return true;
                if (content.Length == 4 && content.SequenceEqual(new byte[] { 0xCC, 0x5A, 0x00, 0x01 })) // Custom marker
                    return true;
            }

            // Additional heuristic: compare file timestamps
            var encFileInfo = new FileInfo(encryptedFilePath);
            return encFileInfo.LastWriteTime > fileInfo.LastWriteTime;
        }
        catch
        {
            return true; // Assume decryption needed on error
        }
    }

    public string GetEncryptedFilePath(
        string decryptedFilePath,
        string decryptedRoot,
        string encryptedRoot)
    {
        try
        {
            string relativePath = Path.GetRelativePath(decryptedRoot, decryptedFilePath);
            return Path.Combine(encryptedRoot, relativePath + ".encrypted");
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task CreateEnhancedPlaceholderFileAsync(string filePath, string encryptedPath)
    {
        try
        {
            // Create a placeholder with a custom marker that indicates lazy decryption
            // Format: [MARKER: 4 bytes] = 0xCC 0xZZ 0x00 0x01 (CloudZCrypt lazy marker)
            byte[] placeholderMarker = new byte[] { 0xCC, 0x5A, 0x00, 0x01 }; // CC = CloudZCrypt, ZZ = LaZy
            await File.WriteAllBytesAsync(filePath, placeholderMarker);
            
            // Set file attributes to indicate it's a placeholder
            File.SetAttributes(filePath, FileAttributes.ReadOnly | FileAttributes.Hidden);
        }
        catch
        {
            // Fallback to simple placeholder
            try
            {
                await File.WriteAllBytesAsync(filePath, new byte[] { 0x00 });
            }
            catch
            {
                // If we can't create the placeholder, just skip it
            }
        }
    }

    private async Task<bool> IsFileFullyDecryptedAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            // Quick cache check
            if (_fileDecryptionCache.TryGetValue(filePath, out var cachedInfo) && cachedInfo.IsDecrypted)
                return true;

            var fileInfo = new FileInfo(filePath);
            
            // If file is larger than our placeholder markers, it's likely been fully decrypted
            if (fileInfo.Length > 4)
                return true;
                
            // Check if it still contains placeholder markers
            if (fileInfo.Length == 4)
            {
                byte[] content = await File.ReadAllBytesAsync(filePath);
                return !content.SequenceEqual(new byte[] { 0xCC, 0x5A, 0x00, 0x01 });
            }
            
            if (fileInfo.Length == 1)
            {
                byte[] content = await File.ReadAllBytesAsync(filePath);
                return content[0] != 0x00;
            }

            return fileInfo.Length > 1;
        }
        catch
        {
            return false;
        }
    }

    public void ClearCache()
    {
        _fileDecryptionCache.Clear();
        _decryptedFileTimestamps.Clear();
    }

    public int GetCacheSize()
    {
        return _fileDecryptionCache.Count;
    }
}

internal class FileDecryptionInfo
{
    public string EncryptedPath { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDecrypted { get; set; }
}