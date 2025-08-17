using CloudZCrypt.Domain.Entities.FileSystem;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Virtual file system implementation using Windows Subst command and on-demand decryption
/// This provides a more reliable mounting experience than complex virtual file system libraries
/// </summary>
public class FileSystemService(IEncryptionServiceFactory encryptionServiceFactory) : IFileSystemService, IDisposable
{
    private readonly ConcurrentDictionary<string, VolumeInfo> _mountedVolumes = new();

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

            IEncryptionService encryptionService = encryptionServiceFactory.Create(encryptionAlgorithm);

            // Create a temporary directory for decrypted content
            string tempDir = CreateTemporaryMountDirectory(mountPoint);

            // Decrypt all files initially
            await DecryptVaultToDirectory(encryptedDirectoryPath, tempDir, password, encryptionService, keyDerivationAlgorithm);

            // Create file system watcher for real-time changes
            FileSystemWatcher watcher = CreateFileSystemWatcher(tempDir, encryptedDirectoryPath, password, encryptionService, keyDerivationAlgorithm);

            // Mount as network drive using subst command
            bool success = await MountAsNetworkDrive(mountPoint, tempDir);
            if (!success)
            {
                watcher?.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }

            // Create volume configuration value object
            VolumeConfiguration configuration = new(
                encryptedDirectoryPath,
                tempDir,
                password,
                keyDerivationAlgorithm);

            // Store volume information as entity
            VolumeInfo volumeInfo = new(
                configuration,
                DateTime.UtcNow,
                encryptionService,
                watcher);

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
            if (!_mountedVolumes.TryRemove(mountPoint, out VolumeInfo? volumeInfo))
            {
                // Try to unmount even if not tracked (for cleanup scenarios)
                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
                return true;
            }

            // Stop file system watcher
            volumeInfo.DisableWatcher();

            // Sync any remaining changes back to encrypted vault
            await SyncChangesToVault(
                volumeInfo.Configuration.TempDirectory,
                volumeInfo.Configuration.EncryptedDirectory,
                volumeInfo.Configuration.Password,
                volumeInfo.EncryptionService,
                volumeInfo.Configuration.KeyDerivationAlgorithm);

            // Unmount the drive
            await UnmountNetworkDrive(mountPoint);

            // Clean up temporary directory
            await CleanupTemporaryDirectory(volumeInfo.Configuration.TempDirectory);

            // Dispose resources
            volumeInfo.Dispose();

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
        List<string> mountPoints = _mountedVolumes.Keys.ToList();
        foreach (string? mountPoint in mountPoints)
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
        List<string> mountPoints = _mountedVolumes.Keys.ToList();
        foreach (string? mountPoint in mountPoints)
        {
            try
            {
                // Try synchronous cleanup for dispose
                VolumeInfo volumeInfo = _mountedVolumes[mountPoint];
                volumeInfo.Dispose();

                // Quick unmount attempt
                Process process = new()
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
                if (Directory.Exists(volumeInfo.Configuration.TempDirectory))
                {
                    Directory.Delete(volumeInfo.Configuration.TempDirectory, true);
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
        string tempDir = Path.Combine(Path.GetTempPath(), "CloudZCrypt", $"Mount_{mountPoint.Replace(":", "")}_{DateTime.UtcNow.Ticks}");
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
                string tempBase = Path.Combine(Path.GetTempPath(), "CloudZCrypt");
                if (Directory.Exists(tempBase))
                {
                    string mountPrefix = $"Mount_{pathOrMountPoint.Replace(":", "")}_";
                    string[] matchingDirs = Directory.GetDirectories(tempBase, mountPrefix + "*");
                    foreach (string dir in matchingDirs)
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
        string[] encryptedFiles = Directory.GetFiles(encryptedDirectoryPath, "*.encrypted", SearchOption.AllDirectories);

        foreach (string encryptedFile in encryptedFiles)
        {
            try
            {
                string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedFile);
                string decryptedFileName = relativePath.Replace(".encrypted", "");
                string decryptedFilePath = Path.Combine(decryptedDirectoryPath, decryptedFileName);

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(decryptedFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Decrypt the file
                bool success = await encryptionService.DecryptFileAsync(encryptedFile, decryptedFilePath, password, keyDerivationAlgorithm);
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
        string[] encryptedDirs = Directory.GetDirectories(encryptedDirectoryPath, "*", SearchOption.AllDirectories);
        foreach (string encryptedDir in encryptedDirs)
        {
            string relativePath = Path.GetRelativePath(encryptedDirectoryPath, encryptedDir);
            string decryptedDirPath = Path.Combine(decryptedDirectoryPath, relativePath);
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
        FileSystemWatcher watcher = new(tempDir)
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

                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
                string encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

                // Ensure encrypted directory exists
                string? encryptedDirPath = Path.GetDirectoryName(encryptedPath);
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
            string oldRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath));
            string newRelativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));

            string oldEncryptedPath = Path.Combine(encryptedDir, oldRelativePath + ".encrypted");
            string newEncryptedPath = Path.Combine(encryptedDir, newRelativePath + ".encrypted");

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
            string relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
            string encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

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
            Process process = new()
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
            Process process = new()
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
            string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string relativePath = Path.GetRelativePath(tempDir, file);
                string encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");

                // Ensure encrypted directory exists
                string? encryptedDirPath = Path.GetDirectoryName(encryptedPath);
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