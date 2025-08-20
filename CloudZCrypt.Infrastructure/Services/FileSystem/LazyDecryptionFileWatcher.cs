using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

public class LazyDecryptionFileWatcher : IDisposable
{
    private readonly ILazyDecryptionService _lazyDecryptionService;
    private readonly string _mountDirectory;
    private readonly string _encryptedDirectory;
    private readonly string _password;
    private readonly IEncryptionService _encryptionService;
    private readonly KeyDerivationAlgorithm _keyDerivationAlgorithm;
    private readonly FileSystemWatcher _watcher;
    private readonly FileSystemWatcher _changeWatcher;
    private bool _disposed = false;

    public LazyDecryptionFileWatcher(
        ILazyDecryptionService lazyDecryptionService,
        string mountDirectory,
        string encryptedDirectory,
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        _lazyDecryptionService = lazyDecryptionService;
        _mountDirectory = mountDirectory;
        _encryptedDirectory = encryptedDirectory;
        _password = password;
        _encryptionService = encryptionService;
        _keyDerivationAlgorithm = keyDerivationAlgorithm;

        // Watcher for file access attempts (read operations)
        _watcher = new FileSystemWatcher(_mountDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.CreationTime
        };

        // Separate watcher for file changes that need to be encrypted back
        _changeWatcher = new FileSystemWatcher(_mountDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        _watcher.Created += OnFileAccessed;
        _changeWatcher.Created += OnFileSystemChanged;
        _changeWatcher.Changed += OnFileSystemChanged;
        _changeWatcher.Renamed += OnFileSystemRenamed;
        _changeWatcher.Deleted += OnFileSystemDeleted;

        EnableRaisingEvents = false; // Start disabled, will be enabled explicitly
    }

    public bool EnableRaisingEvents
    {
        get => _watcher.EnableRaisingEvents;
        set
        {
            _watcher.EnableRaisingEvents = value;
            _changeWatcher.EnableRaisingEvents = value;
        }
    }

    private async void OnFileAccessed(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;

        try
        {
            // Only handle file reads, not directory operations
            if (!File.Exists(e.FullPath)) return;

            string encryptedPath = _lazyDecryptionService.GetEncryptedFilePath(
                e.FullPath, _mountDirectory, _encryptedDirectory);

            if (await _lazyDecryptionService.RequiresDecryptionAsync(encryptedPath, e.FullPath))
            {
                // Perform lazy decryption
                await _lazyDecryptionService.DecryptFileOnDemandAsync(
                    encryptedPath, e.FullPath, _password, _encryptionService, _keyDerivationAlgorithm);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid breaking the file system operations
        }
    }

    private async void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;

        if (File.Exists(e.FullPath))
        {
            try
            {
                // Small delay to ensure file is fully written
                await Task.Delay(500);

                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
                string encryptedPath = Path.Combine(_encryptedDirectory, relativePath + ".encrypted");

                // Create directory if it doesn't exist
                string? encryptedDirPath = Path.GetDirectoryName(encryptedPath);
                if (!string.IsNullOrEmpty(encryptedDirPath))
                {
                    Directory.CreateDirectory(encryptedDirPath);
                }

                // Encrypt the changed file back to the vault
                await _encryptionService.EncryptFileAsync(e.FullPath, encryptedPath, _password, _keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {
                // Log error but don't throw
            }
        }
    }

    private async void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        if (_disposed) return;

        try
        {
            string oldRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath));
            string newRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));

            string oldEncryptedPath = Path.Combine(_encryptedDirectory, oldRelativePath + ".encrypted");
            string newEncryptedPath = Path.Combine(_encryptedDirectory, newRelativePath + ".encrypted");

            if (File.Exists(oldEncryptedPath))
            {
                File.Move(oldEncryptedPath, newEncryptedPath);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw
        }
    }

    private async void OnFileSystemDeleted(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;

        try
        {
            string relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
            string encryptedPath = Path.Combine(_encryptedDirectory, relativePath + ".encrypted");

            if (File.Exists(encryptedPath))
            {
                File.Delete(encryptedPath);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        
        try
        {
            _watcher?.Dispose();
            _changeWatcher?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}