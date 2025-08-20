namespace CloudZCrypt.Domain.ValueObjects.FileSystem;

/// <summary>
/// Represents metadata for a virtual file in the on-demand decryption system
/// </summary>
public sealed record VirtualFileMetadata
{
    /// <summary>
    /// Path to the original encrypted file
    /// </summary>
    public string EncryptedFilePath { get; }
    
    /// <summary>
    /// Path to the virtual/placeholder file in the mounted directory
    /// </summary>
    public string VirtualFilePath { get; }
    
    /// <summary>
    /// Original file size (unencrypted)
    /// </summary>
    public long OriginalSize { get; }
    
    /// <summary>
    /// Size of the encrypted file
    /// </summary>
    public long EncryptedSize { get; }
    
    /// <summary>
    /// Last modified time of the encrypted file
    /// </summary>
    public DateTime LastModified { get; }
    
    /// <summary>
    /// Whether the file is currently decrypted and cached
    /// </summary>
    public bool IsDecrypted { get; init; }
    
    /// <summary>
    /// When the file was last accessed (for cache cleanup)
    /// </summary>
    public DateTime? LastAccessed { get; init; }

    public VirtualFileMetadata(
        string encryptedFilePath,
        string virtualFilePath,
        long originalSize,
        long encryptedSize,
        DateTime lastModified)
    {
        if (string.IsNullOrWhiteSpace(encryptedFilePath))
            throw new ArgumentException("Encrypted file path cannot be null or empty", nameof(encryptedFilePath));
        
        if (string.IsNullOrWhiteSpace(virtualFilePath))
            throw new ArgumentException("Virtual file path cannot be null or empty", nameof(virtualFilePath));
        
        if (originalSize < 0)
            throw new ArgumentException("Original size cannot be negative", nameof(originalSize));
        
        if (encryptedSize < 0)
            throw new ArgumentException("Encrypted size cannot be negative", nameof(encryptedSize));

        EncryptedFilePath = encryptedFilePath;
        VirtualFilePath = virtualFilePath;
        OriginalSize = originalSize;
        EncryptedSize = encryptedSize;
        LastModified = lastModified;
    }

    /// <summary>
    /// Creates a copy with updated access information
    /// </summary>
    public VirtualFileMetadata WithAccessed(DateTime accessTime, bool isDecrypted = true)
        => this with { LastAccessed = accessTime, IsDecrypted = isDecrypted };
}