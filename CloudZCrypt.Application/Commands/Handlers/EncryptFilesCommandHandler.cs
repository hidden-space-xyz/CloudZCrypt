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
                return Result<FileProcessingResult>.Success(CreateProcessingResult(
                    false, stopwatch.Elapsed, 0, 0, 0, ["No files found in the source directory."]));
            }

            long totalBytes = files.Sum(f => fileOperationsService.GetFileSize(f));
            long processedBytes = 0;

            // Report initial status
            request.Progress?.Report(new FileProcessingStatus(0, files.Length, 0, totalBytes, TimeSpan.Zero));

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
                request.Progress?.Report(new FileProcessingStatus(
                    i + 1, files.Length, processedBytes, totalBytes, stopwatch.Elapsed));
            }

            stopwatch.Stop();

            return Result<FileProcessingResult>.Success(CreateProcessingResult(
                errors.Count == 0,
                stopwatch.Elapsed,
                totalBytes,
                files.Length - errors.Count,
                files.Length,
                errors));
        }
        catch (OperationCanceledException)
        {
            return Result<FileProcessingResult>.Success(CreateProcessingResult(
                false, TimeSpan.Zero, 0, 0, 0, ["Operation was cancelled."]));
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

    private static FileProcessingResult CreateProcessingResult(
        bool isSuccess,
        TimeSpan elapsedTime,
        long totalBytes,
        int processedFiles,
        int totalFiles,
        IEnumerable<string> errors)
    {
        return new FileProcessingResult(
            isSuccess,
            elapsedTime,
            totalBytes,
            processedFiles,
            totalFiles,
            errors);
    }
}
