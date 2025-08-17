using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using System.Diagnostics;

namespace CloudZCrypt.Application.Commands.Handlers;
public class EncryptFilesCommandHandler(
    IEncryptionServiceFactory encryptionServiceFactory,
    IFileProcessingResultFactory fileProcessingResultFactory,
    IFileProcessingStatusFactory fileProcessingStatusFactory,
    IFileProcessingDomainService fileProcessingDomainService,
    IFileOperationsService fileOperationsService) : ICommandHandler<EncryptFilesCommand, Result<FileProcessingResult>>
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

                Domain.ValueObjects.FileProcessing.FileProcessingResult emptyDomainResult =
                    fileProcessingResultFactory.CreateEmpty(stopwatch.Elapsed);

                return Result<FileProcessingResult>.Success(FileProcessingResult.FromDomain(emptyDomainResult));
            }

            long totalBytes = files.Sum(f => fileOperationsService.GetFileSize(f));
            long processedBytes = 0;


            Domain.ValueObjects.FileProcessing.FileProcessingStatus initialStatus =
                fileProcessingStatusFactory.CreateInitial(files.Length, totalBytes);


            request.Progress?.Report(FileProcessingStatus.FromDomain(initialStatus));


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


                Domain.ValueObjects.FileProcessing.FileProcessingStatus progressStatus =
                    fileProcessingStatusFactory.CreateInProgress(
                        processedFiles: i + 1,
                        totalFiles: files.Length,
                        processedBytes: processedBytes,
                        totalBytes: totalBytes,
                        elapsed: stopwatch.Elapsed);


                request.Progress?.Report(FileProcessingStatus.FromDomain(progressStatus));
            }

            stopwatch.Stop();


            Domain.ValueObjects.FileProcessing.FileProcessingResult domainResult = errors.Count == 0
                ? fileProcessingResultFactory.CreateSuccess(
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: totalBytes,
                    processedFiles: files.Length,
                    totalFiles: files.Length)
                : fileProcessingResultFactory.CreatePartialSuccess(
                    elapsedTime: stopwatch.Elapsed,
                    totalBytes: totalBytes,
                    processedFiles: files.Length - errors.Count,
                    totalFiles: files.Length,
                    errors: errors);

            return Result<FileProcessingResult>.Success(FileProcessingResult.FromDomain(domainResult));
        }
        catch (OperationCanceledException)
        {

            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Stop();


            Domain.ValueObjects.FileProcessing.FileProcessingResult cancelledResult =
                fileProcessingResultFactory.CreateCancelled(
                    elapsedTime: stopwatch.Elapsed,
                    processedFiles: 0,
                    totalFiles: 0,
                    processedBytes: 0,
                    totalBytes: 0);

            return Result<FileProcessingResult>.Success(FileProcessingResult.FromDomain(cancelledResult));
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
