# On-Demand Decryption Implementation

## Overview

This implementation refactors CloudZCrypt to use on-demand file decryption instead of decrypting all files when mounting a vault. This approach is similar to VeraCrypt's behavior and provides several benefits:

- **Faster mount times**: No need to decrypt all files upfront
- **Lower memory usage**: Only accessed files are decrypted
- **Better performance**: Especially beneficial for large vaults
- **Reduced I/O**: Only necessary files are processed

## Architecture

### Core Components

1. **IOnDemandDecryptionService**: Interface for managing on-demand decryption
2. **OnDemandDecryptionService**: Implementation that handles:
   - Virtual directory structure creation
   - Placeholder file management  
   - Just-in-time decryption
   - Cache management and cleanup

3. **OnDemandFileSystemWatcher**: Enhanced file system monitoring that:
   - Detects file access attempts
   - Triggers decryption when needed
   - Manages concurrent access

4. **VirtualFileMetadata**: Value object that tracks:
   - Encrypted file location
   - Virtual file status
   - Access timestamps
   - Decryption state

### How It Works

#### Mount Process (New Behavior)
1. Create temporary mount directory
2. Scan encrypted vault and build file metadata
3. Create placeholder files in virtual directory structure
4. Mount virtual directory as drive letter using `subst`
5. Start file access monitoring

#### File Access (On-Demand Decryption)
1. User tries to access a file in the mounted drive
2. FileSystemWatcher detects the access attempt
3. System checks if file is a placeholder
4. If placeholder, decrypt the actual file just-in-time
5. Replace placeholder with decrypted content
6. User gets access to the real file

#### Cache Management
- Decrypted files are cached temporarily
- Unused files are cleaned up after 30 minutes
- Cache size is managed automatically
- Files can be converted back to placeholders when not needed

## Key Differences from Original Implementation

### Before (Original)
```csharp
// Mount process
await DecryptVaultToDirectory(encryptedPath, tempDir, password, service, algorithm);
// ALL files decrypted at mount time
```

### After (On-Demand)
```csharp
// Mount process - only create virtual structure
await decryptionService.CreateVirtualDirectoryStructureAsync(encryptedPath, tempDir);
// Files decrypted only when accessed
```

## Benefits

1. **Performance**: Mounting large vaults is now much faster
2. **Resource Usage**: Lower memory and disk usage
3. **Scalability**: Can handle very large vaults efficiently
4. **User Experience**: Similar to VeraCrypt - immediate mount, decrypt on access

## Implementation Details

### Placeholder Files
- Small (1KB) JSON files that represent encrypted files
- Contain metadata about the original file
- Hidden and temporary attributes set
- Replaced with real content when accessed

### File Access Detection
- Uses FileSystemWatcher to detect file operations
- Handles concurrent access safely
- Prevents duplicate decryption attempts

### Cache Management
- Automatic cleanup of unused files
- Configurable expiration times
- Thread-safe operations
- Memory-efficient metadata tracking

## Usage Example

```csharp
// Mount vault (fast - no decryption yet)
bool mounted = await fileSystemService.MountVolumeAsync(
    encryptedPath, "X:", password, EncryptionAlgorithm.Aes, KeyDerivationAlgorithm.PBKDF2);

// Files appear in X:\ but are placeholders
// When user opens a file, it gets decrypted automatically
```

## Testing

The implementation can be tested using the `OnDemandDecryptionHelper` class which provides:
- Simulation of file access
- Status checking for files (placeholder vs decrypted)
- Directory listing with decryption status

## Limitations

- Requires Windows (uses `subst` command)
- File access detection is best-effort (FileSystemWatcher limitations)
- Some applications may require special handling for placeholder files

## Future Enhancements

- Integration with Windows file system filters for better access detection
- Support for other mounting methods (WinFsp, Dokan)
- Advanced caching strategies
- File access logging and analytics