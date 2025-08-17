using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Domain.Entities.FileSystem;

/// <summary>
/// Entity representing an active volume with its associated services and state
/// </summary>
public class VolumeInfo
{
    public VolumeConfiguration Configuration { get; }
    public DateTime MountedAt { get; }
    public FileSystemWatcher? Watcher { get; private set; }
    public IEncryptionService EncryptionService { get; }

    public VolumeInfo(
        VolumeConfiguration configuration,
        DateTime mountedAt,
        IEncryptionService encryptionService,
        FileSystemWatcher? watcher = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        MountedAt = mountedAt;
        EncryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        Watcher = watcher;
    }

    /// <summary>
    /// Gets the duration since the volume was mounted
    /// </summary>
    public TimeSpan MountDuration => DateTime.UtcNow - MountedAt;

    /// <summary>
    /// Gets whether the volume has an active file system watcher
    /// </summary>
    public bool HasActiveWatcher => Watcher?.EnableRaisingEvents == true;

    /// <summary>
    /// Sets the file system watcher for this volume
    /// </summary>
    public void SetWatcher(FileSystemWatcher? watcher)
    {
        Watcher?.Dispose();
        Watcher = watcher;
    }

    /// <summary>
    /// Enables the file system watcher if one is configured
    /// </summary>
    public void EnableWatcher()
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = true;
        }
    }

    /// <summary>
    /// Disables the file system watcher if one is configured
    /// </summary>
    public void DisableWatcher()
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = false;
        }
    }

    /// <summary>
    /// Disposes of the volume resources
    /// </summary>
    public void Dispose()
    {
        Watcher?.Dispose();
    }
}