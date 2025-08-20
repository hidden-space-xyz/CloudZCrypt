using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using System.Collections.Concurrent;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

public class LazyDecryptionService : ILazyDecryptionService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

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

            foreach (string encryptedFile in encryptedFiles)
            {
                // Create the directory structure
                string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedFile);
                string decryptedFileName = relativePath.Replace(".encrypted", "");
                string decryptedFilePath = Path.Combine(mountDirectoryPath, decryptedFileName);

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(decryptedFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a placeholder/stub file instead of decrypting the full content
                await CreatePlaceholderFileAsync(decryptedFilePath, encryptedFile);
            }

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

            // Perform the actual decryption
            return await encryptionService.DecryptFileAsync(encryptedFilePath, decryptedFilePath, password, keyDerivationAlgorithm);
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
            // Check if encrypted file exists but decrypted file is missing or is a placeholder
            if (!File.Exists(encryptedFilePath))
                return false;

            if (!File.Exists(decryptedFilePath))
                return true;

            // Check if the file is just a placeholder (very small size)
            var fileInfo = new FileInfo(decryptedFilePath);
            if (fileInfo.Length <= 1) // Placeholder files are typically very small
                return true;

            // Could add additional checks here (e.g., file timestamp comparison)
            return false;
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

    private async Task CreatePlaceholderFileAsync(string filePath, string encryptedPath)
    {
        try
        {
            // Create a small placeholder file that indicates the file exists but hasn't been decrypted
            // We write just a single byte as a marker
            await File.WriteAllBytesAsync(filePath, new byte[] { 0x00 });
        }
        catch
        {
            // If we can't create the placeholder, just skip it
        }
    }

    private async Task<bool> IsFileFullyDecryptedAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var fileInfo = new FileInfo(filePath);
            // If file is larger than 1 byte, it's likely been fully decrypted
            return fileInfo.Length > 1;
        }
        catch
        {
            return false;
        }
    }
}