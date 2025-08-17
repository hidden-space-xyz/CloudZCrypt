using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;

namespace CloudZCrypt.Domain.Entities.FileSystem;
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
