using CloudZCrypt.Domain.Entities.FileSystem;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileSystem;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;
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


            string tempDir = CreateTemporaryMountDirectory(mountPoint);


            await DecryptVaultToDirectory(encryptedDirectoryPath, tempDir, password, encryptionService, keyDerivationAlgorithm);


            FileSystemWatcher watcher = CreateFileSystemWatcher(tempDir, encryptedDirectoryPath, password, encryptionService, keyDerivationAlgorithm);


            bool success = await MountAsNetworkDrive(mountPoint, tempDir);
            if (!success)
            {
                watcher?.Dispose();
                Directory.Delete(tempDir, true);
                return false;
            }


            VolumeConfiguration configuration = new(
                encryptedDirectoryPath,
                tempDir,
                password,
                keyDerivationAlgorithm);


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

                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
                return true;
            }


            volumeInfo.DisableWatcher();


            await SyncChangesToVault(
                volumeInfo.Configuration.TempDirectory,
                volumeInfo.Configuration.EncryptedDirectory,
                volumeInfo.Configuration.Password,
                volumeInfo.EncryptionService,
                volumeInfo.Configuration.KeyDerivationAlgorithm);


            await UnmountNetworkDrive(mountPoint);


            await CleanupTemporaryDirectory(volumeInfo.Configuration.TempDirectory);


            volumeInfo.Dispose();

            return true;
        }
        catch (Exception ex)
        {

            try
            {
                await UnmountNetworkDrive(mountPoint);
                await CleanupTemporaryDirectory(mountPoint);
            }
            catch
            {

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

            }
        }
    }

    public void Dispose()
    {

        List<string> mountPoints = _mountedVolumes.Keys.ToList();
        foreach (string? mountPoint in mountPoints)
        {
            try
            {

                VolumeInfo volumeInfo = _mountedVolumes[mountPoint];
                volumeInfo.Dispose();


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


                if (Directory.Exists(volumeInfo.Configuration.TempDirectory))
                {
                    Directory.Delete(volumeInfo.Configuration.TempDirectory, true);
                }
            }
            catch
            {

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


                string? directory = Path.GetDirectoryName(decryptedFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }


                bool success = await encryptionService.DecryptFileAsync(encryptedFile, decryptedFilePath, password, keyDerivationAlgorithm);
                if (!success)
                {

                }
            }
            catch (Exception ex)
            {

            }
        }


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

                await Task.Delay(500);

                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath));
                string encryptedPath = Path.Combine(encryptedDir, relativePath + ".encrypted");


                string? encryptedDirPath = Path.GetDirectoryName(encryptedPath);
                if (!string.IsNullOrEmpty(encryptedDirPath))
                {
                    Directory.CreateDirectory(encryptedDirPath);
                }


                await encryptionService.EncryptFileAsync(e.FullPath, encryptedPath, password, keyDerivationAlgorithm);
            }
            catch (Exception ex)
            {

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


                string? encryptedDirPath = Path.GetDirectoryName(encryptedPath);
                if (!string.IsNullOrEmpty(encryptedDirPath))
                {
                    Directory.CreateDirectory(encryptedDirPath);
                }


                await encryptionService.EncryptFileAsync(file, encryptedPath, password, keyDerivationAlgorithm);
            }
        }
        catch (Exception ex)
        {

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
