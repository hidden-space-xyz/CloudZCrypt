using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Models;
using CloudZCrypt.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CloudZCrypt.Infrastructure.Services.VirtualFileSystem;

/// <summary>
/// Virtual file system implementation using Windows Subst command and on-demand decryption
/// This provides a more reliable mounting experience than complex virtual file system libraries
/// </summary>
public class WinFspVirtualFileSystemService : IVirtualFileSystemService, IDisposable
{
    private readonly IEncryptionServiceFactory _encryptionServiceFactory;
    private readonly ConcurrentDictionary<string, VolumeInfo> _mountedVolumes = new();

    private record VolumeInfo(
        string EncryptedDirectory,
        string TempDirectory,
        DateTime MountedAt,
        FileSystemWatcher? Watcher,
        IEncryptionService EncryptionService,
        string Password,
        KeyDerivationAlgorithm KeyDerivationAlgorithm);

    public WinFspVirtualFileSystemService(
        IEncryptionServiceFactory encryptionServiceFactory)
    {
        _encryptionServiceFactory = encryptionServiceFactory;
    }

    public async Task<bool> MountVolumeAsync(
        string encryptedDirectoryPath,
        string mountPoint,
        string password,
        EncryptionAlgorithm encryptionAlgorithm,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            if (_mountedVolumes.ContainsKey(mountPoint))
            {
                return false;
            }

            if (!Directory.Exists(encryptedDirectoryPath))
            {
                return false;
            }

            var encryptionService = _encryptionServiceFactory.Create(encryptionAlgorithm);

            // Create a temporary directory for decrypted content
            var tempDir = CreateTemporaryMountDirectory(mountPoint);
            
            // Decrypt all files initially
            await DecryptVaultToDirectory(encryptedDirectoryPath, tempDir, password, encryptionService, keyDerivationAlgorithm);

            // Create file system watcher for real-time changes
            var watcher = CreateFileSystemWatcher(tempDir, encryptedDirectoryPath, password, encryptionService, keyDerivationAlgorithm);

            // Mount as network drive using subst command
            var success = await MountAsNetworkDrive(mountPoint, tempDir);
            if (!success)
            {
                watcher?.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }

            // Store volume information
            var volumeInfo = new VolumeInfo(
                encryptedDirectoryPath,
                tempDir,
                DateTime.UtcNow,
                watcher,
                encryptionService,
                password,
                keyDerivationAlgorithm);

            _mountedVolumes[mountPoint] = volumeInfo;

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> UnmountVolumeAsync(string mountPoint)
    {
        try
        {
            if (!_mountedVolumes.TryRemove(mountPoint, out var volumeInfo))
            {
                // Try to unmount even if not tracked (for cleanup scenarios)
                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
                return true;
            }

            // Stop file system watcher
            volumeInfo.Watcher?.Dispose();

            // Sync any remaining changes back to encrypted vault
            await SyncChangesToVault(volumeInfo.TempDirectory, volumeInfo.EncryptedDirectory, 
                volumeInfo.Password, volumeInfo.EncryptionService, volumeInfo.KeyDerivationAlgorithm);

            // Unmount the drive
            await UnmountNetworkDrive(mountPoint);

            // Clean up temporary directory
            await CleanupTemporaryDirectory(volumeInfo.TempDirectory);

            return true;
        }
        catch (Exception ex)
        {
            // Always try to cleanup even if errors occur
            try
            {
                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
            }
            catch
            {
                // Ignore cleanup errors
            }
            return false;
        }
    }

    public IEnumerable<string> GetMountedVolumes()
    {
        return _mountedVolumes.Keys.ToList();
    }

    public bool IsMounted(string mountPoint)
    {
        return _mountedVolumes.ContainsKey(mountPoint);
    }

    public async Task UnmountAllVolumesAsync()
    {
        var mountPoints = _mountedVolumes.Keys.ToList();
        foreach (var mountPoint in mountPoints)
        {
            try
            {
                await UnmountVolumeAsync(mountPoint);
            }
            catch
            {
                // Continue with other volumes even if one fails
            }
        }
    }

    public void Dispose()
    {
        // Emergency cleanup - try to unmount all volumes without waiting
        var mountPoints = _mountedVolumes.Keys.ToList();
        foreach (var mountPoint in mountPoints)
        {
            try
            {
                // Try synchronous cleanup for dispose
                var volumeInfo = _mountedVolumes[mountPoint];
                volumeInfo.Watcher?.Dispose();
                
                // Quick unmount attempt
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "subst",
                        Arguments = $"{mountPoint} /d",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit(2000); // Wait max 2 seconds

                // Quick cleanup attempt
                if (Directory.Exists(volumeInfo.TempDirectory))
                {
                    Directory.Delete(volumeInfo.TempDirectory, true);
                }
            }
            catch
            {
                // Ignore errors during dispose
            }
        }
        _mountedVolumes.Clear();
    }

    private string CreateTemporaryMountDirectory(string mountPoint)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CloudZCrypt", $"Mount_{mountPoint.Replace(":", "")}_{DateTime.UtcNow.Ticks}");
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private async Task CleanupTemporaryDirectory(string pathOrMountPoint)
    {
        try
        {
            // If it's a mount point, find the temp directory
            if (pathOrMountPoint.Length == 2 && pathOrMountPoint.EndsWith(":"))
            {
                var tempBase = Path.Combine(Path.GetTempPath(), "CloudZCrypt");
                if (Directory.Exists(tempBase))
                {
                    var mountPrefix = $"Mount_{pathOrMountPoint.Replace(":", "")}_";
                    var matchingDirs = Directory.GetDirectories(tempBase, mountPrefix + "*");
                    foreach (var dir in matchingDirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch
                        {
                            // Ignore individual directory cleanup errors
                        }
                    }
                }
            }
            else if (Directory.Exists(pathOrMountPoint))
            {
                // Direct path cleanup
                Directory.Delete(pathOrMountPoint, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private async Task DecryptVaultToDirectory(
        string encryptedDirectoryPath, 
        string decryptedDirectoryPath, 
        string password, 
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        var encryptedFiles = Directory.GetFiles(encryptedDirectoryPath, "*.encrypted", SearchOption.AllDirectories);

        foreach (var encryptedFile in encryptedFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedFile);
                var decryptedFileName = relativePath.Replace(".encrypted", "");
                var decryptedFilePath = Path.Combine(decryptedDirectoryPath, decryptedFileName);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(decryptedFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Decrypt the file
                var success = await encryptionService.DecryptFileAsync(encryptedFile, decryptedFilePath, password, keyDerivationAlgorithm);
                if (!success)
                {
                    // Log error but continue with other files
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other files
            }
        }

        // Copy directory structure
        var encryptedDirs = Directory.GetDirectories(encryptedDirectoryPath, "*", SearchOption.AllDirectories);
        foreach (var encryptedDir in encryptedDirs)
        {
            var relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedDir);
            var decryptedDirPath = Path.Combine(decryptedDirectoryPath, relativePath);
            Directory.CreateDirectory(decryptedDirPath);
        }
    }

    private FileSystemWatcher CreateFileSystemWatcher(
        string tempDir, 
        string encryptedDir, 
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        var watcher = new FileSystemWatcher(tempDir)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Created += async (s, e) => await OnFileSystemChanged(e, encryptedDir, password, encryptionService, keyDerivationAlgorithm);
        watcher.Changed += async (s, e) => await OnFileSystemChanged(e, encryptedDir, password, encryptionService, keyDerivationAlgorithm);
        watcher.Renamed += async (s, e) => await OnFileSystemRenamed(e, encryptedDir, password, encryptionService, keyDerivationAlgorithm);
        watcher.Deleted += async (s, e) => await OnFileSystemDeleted(e, encryptedDir);

        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private async Task OnFileSystemChanged(FileSystemEventArgs e, string encryptedDir, string password, IEncryptionService encryptionService, KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        if (File.Exists(e.FullPath))
        {
            try
            {
                // Wait a bit to ensure file is fully written
                await Task.Delay(500);
                
                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
                var encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

                // Ensure encrypted directory exists
                var encryptedDirPath = Path.GetDirectoryName(encryptedPath);
                if (!string.IsNullOrEmpty(encryptedDirPath))
                {
                    Directory.CreateDirectory(encryptedDirPath);
                }

                // Encrypt the changed file
                await encryptionService.EncryptFileAsync(e.FullPath, encryptedPath, password, keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {
                // Log error but don't throw
            }
        }
    }

    private async Task OnFileSystemRenamed(RenamedEventArgs e, string encryptedDir, string password, IEncryptionService encryptionService, KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            var oldRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath));
            var newRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
            
            var oldEncryptedPath = Path.Combine(encryptedDir, oldRelativePath + ".encrypted");
            var newEncryptedPath = Path.Combine(encryptedDir, newRelativePath + ".encrypted");

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

    private async Task OnFileSystemDeleted(FileSystemEventArgs e, string encryptedDir)
    {
        try
        {
            var relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
            var encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

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

    private async Task<bool> MountAsNetworkDrive(string mountPoint, string targetPath)
    {
        try
        {
            // Use subst command to create a drive mapping
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "subst",
                    Arguments = $"{mountPoint} \"{targetPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<bool> UnmountNetworkDrive(string mountPoint)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "subst",
                    Arguments = $"{mountPoint} /d",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task SyncChangesToVault(
        string tempDir, 
        string encryptedDir, 
        string password,
        IEncryptionService encryptionService,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        try
        {
            var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(tempDir, file);
                var encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

                // Ensure encrypted directory exists
                var encryptedDirPath = Path.GetDirectoryName(encryptedPath);
                if (!string.IsNullOrEmpty(encryptedDirPath))
                {
                    Directory.CreateDirectory(encryptedDirPath);
                }

                // Encrypt the file
                await encryptionService.EncryptFileAsync(file, encryptedPath, password, keyDerivationAlgorithm);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw during cleanup
        }
    }
}