namespace CloudZCrypt.Domain.Models;

/// <summary>
/// Represents a mounted encrypted volume
/// </summary>
public record MountedVolume
{
    public required string MountPoint { get; init; }
    public required string EncryptedDirectoryPath { get; init; }
    public required DateTime MountedAt { get; init; }
    public bool IsMounted { get; init; }
}