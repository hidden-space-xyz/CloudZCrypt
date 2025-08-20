using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Enhanced file system watcher that detects file access attempts and triggers on-demand decryption
/// Uses a practical approach based on FileSystemWatcher events and file access patterns
/// </summary>
public class OnDemandFileSystemWatcher : IDisposable
{
    private readonly IOnDemandDecryptionService _decryptionService;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _processingLock = new(1);
    private readonly HashSet<string> _currentlyProcessing = new();
    private bool _disposed;

    public OnDemandFileSystemWatcher(string watchPath, IOnDemandDecryptionService decryptionService)
    {
        _decryptionService = decryptionService ?? throw new ArgumentNullException(nameof(decryptionService));
        
        _watcher = new FileSystemWatcher(watchPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = false
        };

        // Hook into file system events that typically indicate file access
        _watcher.Created += OnFileAccessed;
        _watcher.Changed += OnFileAccessed;
    }

    public void Start()
    {
        if (!_disposed)
        {
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void Stop()
    {
        if (!_disposed)
        {
            _watcher.EnableRaisingEvents = false;
        }
    }

    private async void OnFileAccessed(object sender, FileSystemEventArgs e)
    {
        if (_disposed || string.IsNullOrEmpty(e.FullPath))
            return;

        await ProcessFileAccessAsync(e.FullPath);
    }

    /// <summary>
    /// Manually trigger decryption check for a specific file
    /// This can be called by external code when it detects file access
    /// </summary>
    public async Task TriggerDecryptionCheckAsync(string filePath)
    {
        if (!_disposed)
        {
            await ProcessFileAccessAsync(filePath);
        }
    }

    private async Task ProcessFileAccessAsync(string filePath)
    {
        if (_disposed)
            return;

        await _processingLock.WaitAsync();
        try
        {
            // Prevent duplicate processing of the same file
            if (_currentlyProcessing.Contains(filePath))
                return;

            _currentlyProcessing.Add(filePath);

            try
            {
                // Check if this is a placeholder file that needs decryption
                if (_decryptionService.IsPlaceholderFile(filePath))
                {
                    await _decryptionService.DecryptFileOnDemandAsync(filePath);
                }
            }
            finally
            {
                _currentlyProcessing.Remove(filePath);
            }
        }
        catch
        {
            // Ignore errors in file processing
        }
        finally
        {
            _processingLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        _watcher?.Dispose();
        _processingLock?.Dispose();
        _currentlyProcessing.Clear();

        GC.SuppressFinalize(this);
    }
}