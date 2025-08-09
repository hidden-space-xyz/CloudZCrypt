using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Application.Interfaces.Encryption;
using CloudZCrypt.Application.Interfaces.Files;
using System.Diagnostics;

namespace CloudZCrypt.Infrastructure.Files;

internal class FileProcessingService : IFileProcessingService
{
    public async Task<FileProcessingResult> ProcessFilesAsync(
        FileProcessingRequest request,
        IEncryptionService encryptionService,
        IProgress<FileEncryptionProcessStatus>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<string> errors = [];

        string[] files = Directory.GetFiles(request.SourceDirectory, "*.*", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            return new FileProcessingResult(
                IsSuccess: false,
                ElapsedTime: stopwatch.Elapsed,
                TotalBytes: 0,
                ProcessedFiles: 0,
                TotalFiles: 0,
                Errors: ["No files found in the source directory."]);
        }

        long totalBytes = files.Sum(f => new FileInfo(f).Length);
        long processedBytes = 0;

        for (int i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string file = files[i];
            string relativePath = Path.GetRelativePath(request.SourceDirectory, file);
            string destinationFilePath = Path.Combine(request.DestinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);

            Task<bool> operation = request.CryptOperation == CryptOperation.Encrypt
                ? encryptionService.EncryptFileAsync(file, destinationFilePath, request.Password)
                : encryptionService.DecryptFileAsync(file, destinationFilePath, request.Password);

            if (!await operation)
            {
                errors.Add(file);
            }

            processedBytes += new FileInfo(file).Length;

            progress?.Report(new FileEncryptionProcessStatus(
                ProcessedFiles: i + 1,
                TotalFiles: files.Length,
                ProcessedBytes: processedBytes,
                TotalBytes: totalBytes,
                Elapsed: stopwatch.Elapsed));
        }

        stopwatch.Stop();

        return new FileProcessingResult(
            IsSuccess: errors.Count == 0,
            ElapsedTime: stopwatch.Elapsed,
            TotalBytes: totalBytes,
            ProcessedFiles: files.Length - errors.Count,
            TotalFiles: files.Length,
            Errors: errors);
    }
}