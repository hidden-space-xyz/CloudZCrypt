# CloudZCrypt - Cryptomator-style File Encryption

CloudZCrypt has been enhanced to work like Cryptomator, providing transparent file encryption with virtual volume mounting capabilities.

## Features

### 1. **Vault Creation**
- Create encrypted vaults from source directories
- Files are encrypted individually with `.encrypted` extension
- Directory structure is preserved
- Multiple encryption algorithms supported (AES, Twofish, Serpent, ChaCha20, Camellia)
- Multiple key derivation functions (PBKDF2, Argon2id)

### 2. **Smart Virtual Drive Mounting**
- Mount encrypted vaults as virtual drives (Z:, Y:, X:, etc.)
- **Real-time synchronization**: Changes are automatically encrypted back to the vault
- **Transparent access**: Work with files as if they were unencrypted
- **File system watching**: Automatic detection of file changes, creations, deletions
- **Background encryption**: Modified files are encrypted in the background
- **Reliable mounting**: Uses Windows' built-in SUBST command for stable drive mapping

### 3. **Cloud Synchronization Ready**
- Individual file encryption allows efficient cloud sync
- Only modified files need to be re-uploaded
- No single large vault file
- Perfect for services like Google Drive, Dropbox, OneDrive

## How to Use

### Creating a Vault

1. **Select Source Directory**: Choose the directory containing files you want to encrypt
2. **Select Vault Directory**: Choose where the encrypted vault will be stored (this is what you sync to the cloud)
3. **Set Password**: Create a strong password (use the generator for best security)
4. **Choose Encryption Settings**: Select encryption and key derivation algorithms
5. **Click "Create Vault"**: Files will be encrypted to the vault directory with `.encrypted` extensions

### Mounting a Vault

1. **Select Vault Directory**: Choose the directory containing encrypted files (.encrypted files)
2. **Enter Password**: Provide the vault password
3. **Choose Drive Letter**: Select an available drive letter (Z:, Y:, X:, W:, V:)
4. **Click "Mount Vault"**: 
   - A new drive appears in "This PC"
   - All files are decrypted and accessible
   - You can work with files normally (open, edit, save, create, delete)
   - Changes are automatically encrypted back to the vault

### Working with Mounted Vaults

Once mounted, you can:
- **Open the drive** in Windows Explorer
- **Edit files** directly with any application
- **Create new files** and folders
- **Delete files** (they'll be removed from the vault too)
- **Rename files** and folders
- All changes are **automatically synchronized** back to the encrypted vault

### Unmounting a Vault

1. **Click "Unmount Vault"** or **"Unmount All"**
2. Final synchronization ensures all changes are saved
3. Drive disappears from "This PC"
4. Only encrypted files remain in the vault directory

## Technical Implementation

### Smart Mounting System

Instead of complex virtual file systems, CloudZCrypt uses:
- **Windows SUBST command**: Creates reliable drive mappings
- **FileSystemWatcher**: Monitors file changes in real-time
- **Automatic encryption**: Background processes handle encryption/decryption
- **Temporary workspace**: Decrypted files in secure temporary location

### Real-time Synchronization

- **File Created**: Automatically encrypted and added to vault
- **File Modified**: Re-encrypted with latest changes
- **File Renamed**: Encrypted file renamed in vault
- **File Deleted**: Encrypted file removed from vault
- **Folder Operations**: Directory structure changes reflected in vault

## Architecture

### Core Components

- **WinFspVirtualFileSystemService**: Handles smart virtual drive mounting
- **IFileEncryptionApplicationService**: Manages file encryption operations
- **IEncryptionService**: Provides encryption algorithms
- **IKeyDerivationService**: Handles password-based key derivation
- **FileSystemWatcher**: Real-time file monitoring

### File Structure

```
Vault Directory/ (sync this to cloud)
??? file1.txt.encrypted
??? file2.pdf.encrypted
??? subdirectory/
?   ??? file3.doc.encrypted
?   ??? file4.jpg.encrypted
??? ...

Mounted Drive (Z:)
??? file1.txt ? transparent access
??? file2.pdf ? transparent access
??? subdirectory/
?   ??? file3.doc ? transparent access
?   ??? file4.jpg ? transparent access
??? ...
```

## Advantages over Traditional Solutions

### Vs. Encrypted Archives (ZIP, 7-Zip)
- ? **Individual file access** without extracting entire archive
- ? **Efficient cloud sync** - only changed files upload
- ? **No size limitations** - works with any vault size
- ? **Real-time updates** - changes sync immediately

### Vs. Disk Encryption (BitLocker, VeraCrypt)
- ? **Cloud storage friendly** - works with any cloud service
- ? **Cross-platform vault** - encrypted files can be accessed from different devices
- ? **Selective encryption** - only encrypt what you want
- ? **No admin rights** required for mounting

### Vs. Complex Virtual File Systems
- ? **More stable** - uses built-in Windows features
- ? **Better performance** - direct file system operations
- ? **Easier troubleshooting** - standard Windows tools work
- ? **No special drivers** required

## Security Features

- **Individual File Encryption**: Each file encrypted separately
- **Strong Key Derivation**: Uses PBKDF2 or Argon2id
- **Multiple Algorithms**: Industry-standard encryption (AES, Twofish, Serpent, ChaCha20, Camellia)
- **Secure Password Generation**: Built-in strong password generator
- **Memory Safety**: Temporary decrypted files are securely deleted
- **Authentication**: Each encrypted file includes authentication tags

## System Requirements

- **Windows 10/11**
- **.NET 9.0 Runtime**
- **Available drive letter** for mounting
- **Cloud storage service** (optional, for synchronization)

## Best Practices

1. **Regular Backups**: Keep backups of your encrypted vault
2. **Strong Passwords**: Use the built-in password generator
3. **Cloud Sync**: Store encrypted vault in cloud storage for access anywhere
4. **Clean Unmounting**: Always unmount properly to ensure all changes are saved
5. **Monitor Mount Status**: Check the mounted volumes list to see active vaults

## Cloud Provider Setup

### Google Drive
1. Install Google Drive for Desktop
2. Create vault in Google Drive folder
3. Mount vault - changes sync automatically

### Dropbox
1. Install Dropbox Desktop
2. Create vault in Dropbox folder  
3. Mount vault - file changes sync in real-time

### OneDrive
1. Already integrated with Windows
2. Create vault in OneDrive folder
3. Mount vault - seamless synchronization

This approach provides the best of both worlds: the security of individual file encryption with the convenience of transparent access through virtual drive mounting.