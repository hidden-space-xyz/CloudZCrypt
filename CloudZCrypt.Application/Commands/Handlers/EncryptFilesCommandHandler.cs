using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using System.Diagnostics;

namespace CloudZCrypt.Application.Commands.Handlers;

/// <summary>
/// Handler for the EncryptFilesCommand
/// </summary>
public class EncryptFilesCommandHandler(IEncryptionServiceFactory encryptionServiceFactory) : ICommandHandler<EncryptFilesCommand, Result<FileProcessingResult>>
{
    public async Task<Result<FileProcessingResult>> Handle(EncryptFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> errors = [];

            string[] files = Directory.GetFiles(request.SourceDirectory, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                FileProcessingResult emptyResult = new(
                    IsSuccess: false,
                    ElapsedTime: stopwatch.Elapsed,
                    TotalBytes: 0,
                    ProcessedFiles: 0,
                    TotalFiles: 0,
                    Errors: ["No files found in the source directory."]);

                return Result<FileProcessingResult>.Success(emptyResult);
            }

            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            long processedBytes = 0;

            // Report initial progress
            request.Progress?.Report(new FileProcessingStatus(
                ProcessedFiles: 0,
                TotalFiles: files.Length,
                ProcessedBytes: processedBytes,
                TotalBytes: totalBytes,
                Elapsed: stopwatch.Elapsed));

            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(request.DestinationDirectory);

            IEncryptionService encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string file = files[i];
                string relativePath = Path.GetRelativePath(request.SourceDirectory, file);
                string destinationFilePath = Path.Combine(request.DestinationDirectory, relativePath);

                // Ensure destination directory exists
                string? destinationDir = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                bool operationResult = await ExecuteFileOperation(
                    encryptionService,
                    request,
                    file,
                    destinationFilePath);

                if (!operationResult)
                {
                    errors.Add($"Failed to process file: {file}");
                }

                processedBytes += new FileInfo(file).Length;

                // Report progress
                request.Progress?.Report(new FileProcessingStatus(
                    ProcessedFiles: i + 1,
                    TotalFiles: files.Length,
                    ProcessedBytes: processedBytes,
                    TotalBytes: totalBytes,
                    Elapsed: stopwatch.Elapsed));
            }

            stopwatch.Stop();

            FileProcessingResult result = new(
                IsSuccess: errors.Count == 0,
                ElapsedTime: stopwatch.Elapsed,
                TotalBytes: totalBytes,
                ProcessedFiles: files.Length - errors.Count,
                TotalFiles: files.Length,
                Errors: errors);

            return Result<FileProcessingResult>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Result<FileProcessingResult>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<FileProcessingResult>.Failure($"An error occurred: {ex.Message}");
        }
    }

    private static Task<bool> ExecuteFileOperation(
        IEncryptionService encryptionService,
        EncryptFilesCommand request,
        string sourceFile,
        string destinationFile)
    {
        return request.EncryptOperation switch
        {
            EncryptOperation.Encrypt => encryptionService.EncryptFileAsync(
                sourceFile,
                destinationFile,
                request.Password,
                request.KeyDerivationAlgorithm),
            EncryptOperation.Decrypt => encryptionService.DecryptFileAsync(
                sourceFile,
                destinationFile,
                request.Password,
                request.KeyDerivationAlgorithm),
            _ => throw new NotSupportedException($"Unsupported operation: {request.EncryptOperation}")
        };
    }
}