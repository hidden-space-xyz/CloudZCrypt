namespace CloudZCrypt.Domain.ValueObjects.FileSystem;
public sealed record MountedVolume
{
    public required string MountPoint { get; init; }
    public required string EncryptedDirectoryPath { get; init; }
    public required DateTime MountedAt { get; init; }
    public bool IsMounted { get; init; }

    public MountedVolume()
    {
    }

    public MountedVolume(
        string mountPoint,
        string encryptedDirectoryPath,
        DateTime mountedAt,
        bool isMounted = true)
    {
        if (string.IsNullOrWhiteSpace(mountPoint))
            throw new ArgumentException("Mount point cannot be null or empty", nameof(mountPoint));

        if (string.IsNullOrWhiteSpace(encryptedDirectoryPath))
            throw new ArgumentException("Encrypted directory path cannot be null or empty", nameof(encryptedDirectoryPath));

        MountPoint = mountPoint;
        EncryptedDirectoryPath = encryptedDirectoryPath;
        MountedAt = mountedAt;
        IsMounted = isMounted;
    }
    public TimeSpan MountDuration => DateTime.UtcNow - MountedAt;
    public MountedVolume WithMountStatus(bool isMounted) => this with { IsMounted = isMounted };
}
