using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Enums;
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
                // Create domain value object first, then map to DTO
                Domain.ValueObjects.FileProcessing.FileProcessingResult emptyDomainResult = new(
                    isSuccess: false,
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: 0,
                    processedFiles: 0,
                    totalFiles: 0,
                    errors: ["No files found in the source directory."]);

                return Result<FileProcessingResult>.Success(FileProcessingResult.FromDomain(emptyDomainResult));
            }

            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            long processedBytes = 0;

            // Create domain value object for progress reporting
            Domain.ValueObjects.FileProcessing.FileProcessingStatus initialStatus = new(
                processedFiles: 0,
                totalFiles: files.Length,
                processedBytes: processedBytes,
                totalBytes: totalBytes,
                elapsed: stopwatch.Elapsed);

            // Report initial progress
            request.Progress?.Report(FileProcessingStatus.FromDomain(initialStatus));

            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(request.DestinationDirectory);

            IEncryptionService encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string file = files[i];
                string relativePath = Path.GetRelativePath(request.SourceDirectory, file);

                // For Cryptomator-style encryption, add .encrypted extension when encrypting
                string destinationFilePath = request.EncryptOperation == EncryptOperation.Encrypt
                    ? Path.Combine(request.DestinationDirectory, relativePath + ".encrypted")
                    : Path.Combine(request.DestinationDirectory, relativePath.Replace(".encrypted", ""));

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

                // Create domain value object for progress reporting
                Domain.ValueObjects.FileProcessing.FileProcessingStatus progressStatus = new(
                    processedFiles: i + 1,
                    totalFiles: files.Length,
                    processedBytes: processedBytes,
                    totalBytes: totalBytes,
                    elapsed: stopwatch.Elapsed);

                // Report progress
                request.Progress?.Report(FileProcessingStatus.FromDomain(progressStatus));
            }

            stopwatch.Stop();

            // Create domain value object for final result
            Domain.ValueObjects.FileProcessing.FileProcessingResult domainResult = new(
                isSuccess: errors.Count == 0,
                elapsedTime: stopwatch.Elapsed,
                totalBytes: totalBytes,
                processedFiles: files.Length - errors.Count,
                totalFiles: files.Length,
                errors: errors);

            return Result<FileProcessingResult>.Success(FileProcessingResult.FromDomain(domainResult));
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