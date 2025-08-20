using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.FileSystem;

/// <summary>
/// Helper service for testing and manually triggering on-demand file decryption
/// This provides a way to simulate file access for demonstration purposes
/// </summary>
public class OnDemandDecryptionHelper
{
    /// <summary>
    /// Simulates file access by checking if a file needs decryption and triggering it
    /// This is useful for demonstrating the on-demand behavior
    /// </summary>
    public static async Task<bool> SimulateFileAccessAsync(string filePath, OnDemandFileSystemWatcher watcher)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            // Trigger the decryption check
            await watcher.TriggerDecryptionCheckAsync(filePath);
            
            // Give it a moment to process
            await Task.Delay(100);
            
            // Check if file was actually decrypted (file size changed from placeholder size)
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length > 1024; // Larger than placeholder size
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the status of a file (whether it's a placeholder or decrypted)
    /// </summary>
    public static string GetFileStatus(string filePath, IOnDemandDecryptionService decryptionService)
    {
        try
        {
            if (!File.Exists(filePath))
                return "File does not exist";

            var fileInfo = new FileInfo(filePath);
            bool isPlaceholder = decryptionService.IsPlaceholderFile(filePath);
            
            if (isPlaceholder)
                return $"Placeholder file (Size: {fileInfo.Length} bytes)";
            else
                return $"Decrypted file (Size: {fileInfo.Length} bytes)";
        }
        catch
        {
            return "Error reading file status";
        }
    }

    /// <summary>
    /// Lists all files in a directory with their decryption status
    /// </summary>
    public static void ListDirectoryStatus(string directoryPath, IOnDemandDecryptionService decryptionService)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory does not exist: {directoryPath}");
                return;
            }

            Console.WriteLine($"Directory contents: {directoryPath}");
            Console.WriteLine("=" + new string('=', directoryPath.Length + 19));

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                string relativePath = Path.GetRelativePath(directoryPath, file);
                string status = GetFileStatus(file, decryptionService);
                Console.WriteLine($"  {relativePath}: {status}");
            }

            if (files.Length == 0)
            {
                Console.WriteLine("  (No files found)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing directory: {ex.Message}");
        }
    }
}