using CloudZCrypt.Domain.Entities.FileSystem;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// FileSystemService with on-demand decryption capability
/// Files are only decrypted when accessed, not at mount time
/// </summary>
public class FileSystemService(IEncryptionServiceFactory encryptionServiceFactory) : IFileSystemService, IDisposable
{
    private readonly ConcurrentDictionary<string, VolumeInfo> _mountedVolumes = new();
    private readonly ConcurrentDictionary<string, IOnDemandDecryptionService> _decryptionServices = new();
    private readonly ConcurrentDictionary<string, OnDemandFileSystemWatcher> _fileWatchers = new();

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

            // Create temporary mount directory
            string tempDir = CreateTemporaryMountDirectory(mountPoint);

            // Initialize on-demand decryption service
            var decryptionService = new OnDemandDecryptionService(encryptionServiceFactory);
            bool initSuccess = await decryptionService.InitializeAsync(
                encryptedDirectoryPath,
                tempDir,
                password,
                encryptionAlgorithm,
                keyDerivationAlgorithm);

            if (!initSuccess)
            {
                decryptionService.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }

            // Create virtual directory structure with placeholder files
            bool structureSuccess = await decryptionService.CreateVirtualDirectoryStructureAsync(
                encryptedDirectoryPath, tempDir);

            if (!structureSuccess)
            {
                decryptionService.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }

            // Create enhanced file system watcher for on-demand decryption
            var fileWatcher = new OnDemandFileSystemWatcher(tempDir, decryptionService);

            // Mount as network drive
            bool mountSuccess = await MountAsNetworkDrive(mountPoint, tempDir);
            if (!mountSuccess)
            {
                fileWatcher.Dispose();
                decryptionService.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }

            // Start file access monitoring
            fileWatcher.Start();

            // Create volume configuration
            VolumeConfiguration configuration = new(
                encryptedDirectoryPath,
                tempDir,
                password,
                keyDerivationAlgorithm);

            // Create volume info (no traditional FileSystemWatcher needed)
            VolumeInfo volumeInfo = new(
                configuration,
                DateTime.UtcNow,
                encryptionService,
                null); // No traditional watcher

            // Store all references
            _mountedVolumes[mountPoint] = volumeInfo;
            _decryptionServices[mountPoint] = decryptionService;
            _fileWatchers[mountPoint] = fileWatcher;

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
            // Stop file access monitoring first
            if (_fileWatchers.TryRemove(mountPoint, out OnDemandFileSystemWatcher? fileWatcher))
            {
                fileWatcher.Stop();
                fileWatcher.Dispose();
            }

            // Get decryption service
            if (_decryptionServices.TryRemove(mountPoint, out IOnDemandDecryptionService? decryptionService))
            {
                // Cleanup cached files
                await decryptionService.CleanupUnusedFilesAsync();
                decryptionService.Dispose();
            }

            // Remove volume info
            if (!_mountedVolumes.TryRemove(mountPoint, out VolumeInfo? volumeInfo))
            {
                // Even if not in our tracking, try to unmount
                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
                return true;
            }

            // Unmount network drive
            await UnmountNetworkDrive(mountPoint);

            // Cleanup temporary directory
            await CleanupTemporaryDirectory(volumeInfo.Configuration.TempDirectory);

            // Dispose volume info
            volumeInfo.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            // Cleanup attempt even on error
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
                // Ignore individual unmount errors
            }
        }
    }

    public void Dispose()
    {
        // Stop all file watchers
        foreach (var watcher in _fileWatchers.Values)
        {
            try
            {
                watcher.Stop();
                watcher.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _fileWatchers.Clear();

        // Dispose all decryption services
        foreach (var service in _decryptionServices.Values)
        {
            try
            {
                service.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _decryptionServices.Clear();

        // Cleanup mounted volumes
        List<string> mountPoints = _mountedVolumes.Keys.ToList();
        foreach (string? mountPoint in mountPoints)
        {
            try
            {
                // Get volume info
                VolumeInfo volumeInfo = _mountedVolumes[mountPoint];
                volumeInfo.Dispose();

                // Unmount drive
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
                process.WaitForExit(2000);

                // Cleanup temp directory
                if (Directory.Exists(volumeInfo.Configuration.TempDirectory))
                {
                    Directory.Delete(volumeInfo.Configuration.TempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
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
            // Handle both direct paths and mount point references
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
                Directory.Delete(pathOrMountPoint, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private async Task<bool> MountAsNetworkDrive(string mountPoint, string targetPath)
    {
        try
        {
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

    public IEnumerable<MountPoint> GetAvailableMountPoints()
    {
        // Get all drive letters currently in use by Windows
        DriveInfo[] drives = DriveInfo.GetDrives();
        HashSet<string> usedDriveLetters = [.. drives.Select(d => d.Name[0].ToString())];

        // Return all MountPoint values that are not in use
        return Enum.GetValues<MountPoint>()
            .Where(mp => !usedDriveLetters.Contains(Enum.GetName(mp)))
            .OrderBy(mp => mp);
    }
}
