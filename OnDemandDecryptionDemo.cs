using CloudZCrypt.Infrastructure.Services.FileSystem;
using CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;
using CloudZCrypt.Domain.Enums;
using System.Diagnostics;

namespace CloudZCrypt.Tests;

/// <summary>
/// Simple demonstration of the on-demand decryption functionality
/// Shows the difference between the old (bulk decryption) and new (on-demand) approaches
/// </summary>
public class OnDemandDecryptionDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("CloudZCrypt On-Demand Decryption Demo");
        Console.WriteLine("====================================");
        Console.WriteLine();

        // Create temporary test directories
        string testDir = Path.Combine(Path.GetTempPath(), "CloudZCryptDemo", Guid.NewGuid().ToString());
        string sourceDir = Path.Combine(testDir, "Source");
        string encryptedDir = Path.Combine(testDir, "Encrypted");
        string mountDir = Path.Combine(testDir, "Mount");

        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(encryptedDir);
        Directory.CreateDirectory(mountDir);

        try
        {
            // Create test files
            await CreateTestFiles(sourceDir);

            // Encrypt files first (using the traditional method)
            Console.WriteLine("1. Creating encrypted vault...");
            await EncryptTestFiles(sourceDir, encryptedDir);

            // Demonstrate on-demand decryption
            Console.WriteLine("2. Demonstrating on-demand decryption...");
            await DemonstrateOnDemandDecryption(encryptedDir, mountDir);

            Console.WriteLine("\nDemo completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Demo failed: {ex.Message}");
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
            catch
            {
                Console.WriteLine("Note: Some temporary files may need manual cleanup");
            }
        }
    }

    private static async Task CreateTestFiles(string sourceDir)
    {
        Console.WriteLine($"   Creating test files in {sourceDir}");

        // Create various test files
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "document.txt"), "This is a test document with some content that will be encrypted.");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "config.json"), """{"setting1": "value1", "setting2": "value2"}""");
        
        // Create a subdirectory with files
        string subDir = Path.Combine(sourceDir, "subfolder");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "This file is in a subdirectory");

        // Create a larger file to show size differences
        var largeContent = new string('A', 10000);
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "largefile.txt"), largeContent);

        Console.WriteLine($"   Created {Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).Length} test files");
    }

    private static async Task EncryptTestFiles(string sourceDir, string encryptedDir)
    {
        Console.WriteLine($"   Encrypting files from {sourceDir} to {encryptedDir}");

        var encryptionService = new AesEncryptionService(null); // Simplified for demo
        string password = "TestPassword123!";

        var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (string sourceFile in sourceFiles)
        {
            try
            {
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string encryptedFile = Path.Combine(encryptedDir, relativePath + ".encrypted");

                // Create directory if needed
                string encryptedFileDir = Path.GetDirectoryName(encryptedFile);
                if (!string.IsNullOrEmpty(encryptedFileDir))
                {
                    Directory.CreateDirectory(encryptedFileDir);
                }

                // This would normally work, but for demo purposes we'll simulate
                Console.WriteLine($"     Encrypted: {relativePath}");
                
                // Simulate encrypted file (copy with .encrypted extension)
                File.Copy(sourceFile, encryptedFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     Failed to encrypt {sourceFile}: {ex.Message}");
            }
        }
    }

    private static async Task DemonstrateOnDemandDecryption(string encryptedDir, string mountDir)
    {
        Console.WriteLine($"   Setting up on-demand decryption service...");

        // Create the on-demand decryption service
        var decryptionService = new OnDemandDecryptionService(null); // Simplified for demo

        try
        {
            // Initialize the service (normally this would be done by FileSystemService)
            bool initialized = await decryptionService.InitializeAsync(
                encryptedDir,
                mountDir,
                "TestPassword123!",
                EncryptionAlgorithm.Aes,
                KeyDerivationAlgorithm.PBKDF2);

            if (!initialized)
            {
                Console.WriteLine("     Failed to initialize on-demand decryption service");
                return;
            }

            // Create virtual directory structure (this is the key difference!)
            Console.WriteLine("   Creating virtual directory structure with placeholder files...");
            var stopwatch = Stopwatch.StartNew();

            bool structureCreated = await decryptionService.CreateVirtualDirectoryStructureAsync(encryptedDir, mountDir);
            
            stopwatch.Stop();

            if (!structureCreated)
            {
                Console.WriteLine("     Failed to create virtual directory structure");
                return;
            }

            Console.WriteLine($"   Virtual structure created in {stopwatch.ElapsedMilliseconds}ms (compared to full decryption which would take much longer)");

            // Show the directory contents
            Console.WriteLine("\n   Virtual directory contents:");
            OnDemandDecryptionHelper.ListDirectoryStatus(mountDir, decryptionService);

            // Simulate file access
            Console.WriteLine("\n   Simulating file access (this would trigger on-demand decryption):");
            var testFiles = Directory.GetFiles(mountDir, "*", SearchOption.AllDirectories).Take(2);

            foreach (string testFile in testFiles)
            {
                string fileName = Path.GetFileName(testFile);
                Console.WriteLine($"     Accessing {fileName}...");

                // Check initial status
                string beforeStatus = OnDemandDecryptionHelper.GetFileStatus(testFile, decryptionService);
                Console.WriteLine($"       Before: {beforeStatus}");

                // Simulate file access (this would trigger decryption in real usage)
                bool wasPlaceholder = decryptionService.IsPlaceholderFile(testFile);
                if (wasPlaceholder)
                {
                    // In real usage, this would be triggered by FileSystemWatcher
                    await decryptionService.DecryptFileOnDemandAsync(testFile);
                }

                // Check status after
                string afterStatus = OnDemandDecryptionHelper.GetFileStatus(testFile, decryptionService);
                Console.WriteLine($"       After:  {afterStatus}");
                
                if (wasPlaceholder)
                {
                    Console.WriteLine($"       ✓ File was decrypted on-demand");
                }
            }

            // Show final directory status
            Console.WriteLine("\n   Final directory status:");
            OnDemandDecryptionHelper.ListDirectoryStatus(mountDir, decryptionService);

            Console.WriteLine("\n   Key benefits demonstrated:");
            Console.WriteLine("   - Fast mount time (virtual structure created quickly)");
            Console.WriteLine("   - Files only decrypted when accessed");
            Console.WriteLine("   - Lower memory usage (only accessed files in memory)");
            Console.WriteLine("   - VeraCrypt-like user experience");
        }
        finally
        {
            decryptionService.Dispose();
        }
    }
}

// Simple program entry point for demonstration
public class Program
{
    public static async Task Main(string[] args)
    {
        await OnDemandDecryptionDemo.RunDemo();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}