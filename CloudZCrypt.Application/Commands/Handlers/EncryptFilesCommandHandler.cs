using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using MediatR;
using System.Diagnostics;

namespace CloudZCrypt.Application.Commands.Handlers;

public class EncryptFilesCommandHandler(
    IEncryptionServiceFactory encryptionServiceFactory,
    IFileProcessingDomainService fileProcessingDomainService,
    IFileOperationsService fileOperationsService) : IRequestHandler<EncryptFilesCommand, Result<FileProcessingResult>>
{
    public async Task<Result<FileProcessingResult>> Handle(EncryptFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            (bool canProceed, IEnumerable<string> validationErrors) = await fileProcessingDomainService
                .ValidateProcessingOperationAsync(request.SourceDirectory, request.DestinationDirectory, cancellationToken);

            if (!canProceed)
            {
                return Result<FileProcessingResult>.Failure(validationErrors.ToArray());
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> errors = [];

            string[] files = await fileOperationsService.GetFilesAsync(request.SourceDirectory, "*.*", cancellationToken);
            if (files.Length == 0)
            {
                stopwatch.Stop();
                FileProcessingResult emptyResult = new(
                    isSuccess: false,
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: 0,
                    processedFiles: 0,
                    totalFiles: 0,
                    errors: ["No files found in the source directory."]);

                return Result<FileProcessingResult>.Success(emptyResult);
            }

            long totalBytes = files.Sum(f => fileOperationsService.GetFileSize(f));
            long processedBytes = 0;

            // Report initial status
            FileProcessingStatus initialStatus = new(0, files.Length, 0, totalBytes, TimeSpan.Zero);
            request.Progress?.Report(initialStatus);

            await fileOperationsService.CreateDirectoryAsync(request.DestinationDirectory, cancellationToken);

            IEncryptionService encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string file = files[i];
                string relativePath = fileOperationsService.GetRelativePath(request.SourceDirectory, file);

                string destinationFilePath = request.EncryptOperation == EncryptOperation.Encrypt
                    ? fileOperationsService.CombinePath(request.DestinationDirectory, relativePath + ".encrypted")
                    : fileOperationsService.CombinePath(request.DestinationDirectory, relativePath.Replace(".encrypted", ""));

                string? destinationDir = fileOperationsService.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    await fileOperationsService.CreateDirectoryAsync(destinationDir, cancellationToken);
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

                processedBytes += fileOperationsService.GetFileSize(file);

                // Report progress
                FileProcessingStatus progressStatus = new(
                    processedFiles: i + 1,
                    totalFiles: files.Length,
                    processedBytes: processedBytes,
                    totalBytes: totalBytes,
                    elapsed: stopwatch.Elapsed);

                request.Progress?.Report(progressStatus);
            }

            stopwatch.Stop();

            FileProcessingResult domainResult = errors.Count == 0
                ? new FileProcessingResult(
                    isSuccess: true,
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: totalBytes,
                    processedFiles: files.Length,
                    totalFiles: files.Length,
                    errors: Array.Empty<string>())
                : new FileProcessingResult(
                    isSuccess: files.Length - errors.Count > 0,
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: totalBytes,
                    processedFiles: files.Length - errors.Count,
                    totalFiles: files.Length,
                    errors: errors);

            return Result<FileProcessingResult>.Success(domainResult);
        }
        catch (OperationCanceledException)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();

            FileProcessingResult cancelledResult = new(
                isSuccess: false,
                elapsedTime: stopwatch.Elapsed,
                totalBytes: 0,
                processedFiles: 0,
                totalFiles: 0,
                errors: ["Operation was cancelled."]);

            return Result<FileProcessingResult>.Success(cancelledResult);
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
