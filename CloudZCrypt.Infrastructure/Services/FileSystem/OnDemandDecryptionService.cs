using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Implementation of on-demand file decryption service that mimics VeraCrypt behavior
/// without using Dokan or other kernel-level drivers
/// </summary>
public class OnDemandDecryptionService : IOnDemandDecryptionService, IDisposable
{
    private readonly IEncryptionServiceFactory _encryptionServiceFactory;
    private readonly ConcurrentDictionary<string, VirtualFileMetadata> _fileMetadata = new();
    private readonly Timer _cleanupTimer;
    private readonly SemaphoreSlim _decryptionSemaphore = new(Environment.ProcessorCount);
    
    private string? _encryptedDirectoryPath;
    private string? _tempDirectoryPath;
    private string? _password;
    private EncryptionAlgorithm _encryptionAlgorithm;
    private KeyDerivationAlgorithm _keyDerivationAlgorithm;
    private IEncryptionService? _encryptionService;
    private bool _disposed;

    // Configuration
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FileExpirationTime = TimeSpan.FromMinutes(30);
    private const string MetadataFileName = ".cloudzcrypt_metadata.json";
    private const long PlaceholderFileSize = 1024; // 1KB placeholder files

    public OnDemandDecryptionService(IEncryptionServiceFactory encryptionServiceFactory)
    {
        _encryptionServiceFactory = encryptionServiceFactory ?? throw new ArgumentNullException(nameof(encryptionServiceFactory));
        _cleanupTimer = new Timer(CleanupCallback, null, CleanupInterval, CleanupInterval);
    }

    public async Task<bool> InitializeAsync(
        string encryptedDirectoryPath,
        string tempDirectoryPath,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encryptedDirectoryPath) || !Directory.Exists(encryptedDirectoryPath))
                return false;

            if (string.IsNullOrWhiteSpace(tempDirectoryPath))
                return false;

            if (string.IsNullOrWhiteSpace(password))
                return false;

            _encryptedDirectoryPath = encryptedDirectoryPath;
            _tempDirectoryPath = tempDirectoryPath;
            _password = password;
            _encryptionAlgorithm = encryptionAlgorithm;
            _keyDerivationAlgorithm = keyDerivationAlgorithm;
            _encryptionService = _encryptionServiceFactory.Create(encryptionAlgorithm);

            // Create temp directory if it doesn't exist
            Directory.CreateDirectory(tempDirectoryPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateVirtualDirectoryStructureAsync(string encryptedDirectoryPath, string virtualDirectoryPath)
    {
        try
        {
            if (!Directory.Exists(encryptedDirectoryPath))
                return false;

            Directory.CreateDirectory(virtualDirectoryPath);

            // Get all encrypted files
            string[] encryptedFiles = Directory.GetFiles(encryptedDirectoryPath, "*.encrypted", SearchOption.AllDirectories);

            foreach (string encryptedFile in encryptedFiles)
            {
                await CreateVirtualFileAsync(encryptedFile, encryptedDirectoryPath, virtualDirectoryPath);
            }

            // Create empty directories that exist in the encrypted vault
            await CreateVirtualDirectoriesAsync(encryptedDirectoryPath, virtualDirectoryPath);

            // Save metadata to disk for persistence
            await SaveMetadataAsync(virtualDirectoryPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DecryptFileOnDemandAsync(string virtualFilePath)
    {
        if (!_fileMetadata.TryGetValue(virtualFilePath, out VirtualFileMetadata? metadata))
            return false;

        await _decryptionSemaphore.WaitAsync();
        try
        {
            // Check if already decrypted and cached
            if (metadata.IsDecrypted && File.Exists(virtualFilePath))
            {
                var fileInfo = new FileInfo(virtualFilePath);
                if (fileInfo.Length > PlaceholderFileSize) // Not a placeholder anymore
                {
                    // Update access time
                    _fileMetadata[virtualFilePath] = metadata.WithAccessed(DateTime.UtcNow);
                    return true;
                }
            }

            if (_encryptionService == null || string.IsNullOrEmpty(_password))
                return false;

            // Decrypt the file
            bool success = await _encryptionService.DecryptFileAsync(
                metadata.EncryptedFilePath,
                virtualFilePath,
                _password,
                _keyDerivationAlgorithm);

            if (success)
            {
                // Update metadata
                _fileMetadata[virtualFilePath] = metadata.WithAccessed(DateTime.UtcNow, true);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _decryptionSemaphore.Release();
        }
    }

    public bool IsPlaceholderFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Check if it's registered in our metadata
            if (!_fileMetadata.TryGetValue(filePath, out VirtualFileMetadata? metadata))
                return false;

            // A file is a placeholder if it's small (placeholder size) and not marked as decrypted
            return fileInfo.Length <= PlaceholderFileSize && !metadata.IsDecrypted;
        }
        catch
        {
            return false;
        }
    }

    public async Task CleanupUnusedFilesAsync()
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - FileExpirationTime;
            var filesToCleanup = _fileMetadata.Values
                .Where(m => m.IsDecrypted && 
                           m.LastAccessed.HasValue && 
                           m.LastAccessed.Value < cutoffTime)
                .ToList();

            foreach (var metadata in filesToCleanup)
            {
                await ConvertBackToPlaceholderAsync(metadata);
            }
        }
        catch
        {
            // Silent cleanup failure
        }
    }

    private async Task CreateVirtualFileAsync(string encryptedFile, string encryptedDirectoryPath, string virtualDirectoryPath)
    {
        try
        {
            // Calculate relative path and remove .encrypted extension
            string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedFile);
            string virtualFileName = relativePath.Replace(".encrypted", "");
            string virtualFilePath = Path.Combine(virtualDirectoryPath, virtualFileName);

            // Create directory if needed
            string? virtualFileDir = Path.GetDirectoryName(virtualFilePath);
            if (!string.IsNullOrEmpty(virtualFileDir))
            {
                Directory.CreateDirectory(virtualFileDir);
            }

            // Get file info
            var encryptedFileInfo = new FileInfo(encryptedFile);
            long originalSize = await EstimateOriginalFileSizeAsync(encryptedFile);

            // Create placeholder file
            await CreatePlaceholderFileAsync(virtualFilePath, originalSize);

            // Store metadata
            var metadata = new VirtualFileMetadata(
                encryptedFile,
                virtualFilePath,
                originalSize,
                encryptedFileInfo.Length,
                encryptedFileInfo.LastWriteTime);

            _fileMetadata[virtualFilePath] = metadata;
        }
        catch
        {
            // Skip this file on error
        }
    }

    private async Task CreateVirtualDirectoriesAsync(string encryptedDirectoryPath, string virtualDirectoryPath)
    {
        try
        {
            string[] encryptedDirs = Directory.GetDirectories(encryptedDirectoryPath, "*", SearchOption.AllDirectories);
            
            foreach (string encryptedDir in encryptedDirs)
            {
                string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedDir);
                string virtualDir = Path.Combine(virtualDirectoryPath, relativePath);
                Directory.CreateDirectory(virtualDir);
            }
        }
        catch
        {
            // Ignore directory creation errors
        }
    }

    private async Task<long> EstimateOriginalFileSizeAsync(string encryptedFilePath)
    {
        try
        {
            var fileInfo = new FileInfo(encryptedFilePath);
            // Estimate: encrypted size minus salt, nonce, and tag overhead
            // This is an approximation since we can't know the exact size without decrypting
            const int EncryptionOverhead = 32 + 12 + 16; // Salt + Nonce + Tag
            return Math.Max(0, fileInfo.Length - EncryptionOverhead);
        }
        catch
        {
            return 0;
        }
    }

    private async Task CreatePlaceholderFileAsync(string filePath, long estimatedSize)
    {
        try
        {
            // Create a small placeholder file with metadata about the real file
            var placeholderData = new
            {
                IsPlaceholder = true,
                EstimatedSize = estimatedSize,
                CreatedAt = DateTime.UtcNow,
                Message = "This file will be decrypted when accessed"
            };

            string jsonContent = JsonSerializer.Serialize(placeholderData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonContent);

            // Set file attributes to indicate it's a placeholder
            File.SetAttributes(filePath, FileAttributes.Hidden | FileAttributes.Temporary);
        }
        catch
        {
            // Create empty file as fallback
            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());
        }
    }

    private async Task ConvertBackToPlaceholderAsync(VirtualFileMetadata metadata)
    {
        try
        {
            if (File.Exists(metadata.VirtualFilePath))
            {
                await CreatePlaceholderFileAsync(metadata.VirtualFilePath, metadata.OriginalSize);
                _fileMetadata[metadata.VirtualFilePath] = metadata with { LastAccessed = null, IsDecrypted = false };
            }
        }
        catch
        {
            // Ignore conversion errors
        }
    }

    private async Task SaveMetadataAsync(string virtualDirectoryPath)
    {
        try
        {
            string metadataPath = Path.Combine(virtualDirectoryPath, MetadataFileName);
            var metadataDict = _fileMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            string json = JsonSerializer.Serialize(metadataDict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, json);
            File.SetAttributes(metadataPath, FileAttributes.Hidden | FileAttributes.System);
        }
        catch
        {
            // Ignore metadata save errors
        }
    }

    private async void CleanupCallback(object? state)
    {
        if (!_disposed)
        {
            await CleanupUnusedFilesAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer?.Dispose();
        _decryptionSemaphore?.Dispose();
        _encryptionService = null;
        
        // Clear sensitive data
        _password = null;
        _fileMetadata.Clear();

        GC.SuppressFinalize(this);
    }
}