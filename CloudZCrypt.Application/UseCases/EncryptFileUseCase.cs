using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using System.Diagnostics;

namespace CloudZCrypt.Application.UseCases
{
    public class EncryptFileUseCase(IEncryptionServiceFactory encryptionServiceFactory)
    {
        public async Task<FileProcessingResult> EncryptFilesAsync(FileProcessingRequest request, IProgress<FileProcessingStatus>? progress = null, CancellationToken cancellationToken = default)
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

            progress?.Report(new FileProcessingStatus(
                ProcessedFiles: 0,
                TotalFiles: files.Length,
                ProcessedBytes: processedBytes,
                TotalBytes: totalBytes,
                Elapsed: stopwatch.Elapsed));

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string file = files[i];
                string relativePath = Path.GetRelativePath(request.SourceDirectory, file);
                string destinationFilePath = Path.Combine(request.DestinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);

                IEncryptionService encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);

                Task<bool> operation = request.EncryptOperation switch
                {
                    EncryptOperation.Encrypt => encryptionService.EncryptFileAsync(file, destinationFilePath, request.Password),
                    EncryptOperation.Decrypt => encryptionService.DecryptFileAsync(file, destinationFilePath, request.Password),
                    _ => throw new NotSupportedException($"Unsupported operation: {request.EncryptOperation}")
                };

                if (!await operation)
                    errors.Add(file);

                processedBytes += new FileInfo(file).Length;

                progress?.Report(new FileProcessingStatus(
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
}
