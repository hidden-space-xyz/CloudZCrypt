using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Domain.Entities.FileSystem;
public class VolumeInfo(
    VolumeConfiguration configuration,
    DateTime mountedAt,
    IEncryptionService encryptionService,
    FileSystemWatcher? watcher = null)
{
    public VolumeConfiguration Configuration { get; } = configuration ?? throw new ArgumentNullException(nameof(configuration));
    public DateTime MountedAt { get; } = mountedAt;
    public FileSystemWatcher? Watcher { get; private set; } = watcher;
    public IEncryptionService EncryptionService { get; } = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));

    public TimeSpan MountDuration => DateTime.UtcNow - MountedAt;
    public bool HasActiveWatcher => Watcher?.EnableRaisingEvents == true;
    public void SetWatcher(FileSystemWatcher? watcher)
    {
        Watcher?.Dispose();
        Watcher = watcher;
    }
    public void EnableWatcher()
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = true;
        }
    }
    public void DisableWatcher()
    {
        if (Watcher != null)
        {
            Watcher.EnableRaisingEvents = false;
        }
    }
    public void Dispose()
    {
        Watcher?.Dispose();
    }
}
