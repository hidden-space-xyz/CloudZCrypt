using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Exceptions;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using MediatR;
using System.Diagnostics;

namespace CloudZCrypt.Application.Commands.Handlers;

public class ProcessFileCommandHandler(
    IEncryptionServiceFactory encryptionServiceFactory,
    IFileProcessingDomainService fileProcessingDomainService,
    IFileOperationsService fileOperationsService) : IRequestHandler<ProcessFileCommand, Result<FileProcessingResult>>
{
    public async Task<Result<FileProcessingResult>> Handle(ProcessFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> errors = [];

            // Check if source is a file or directory
            bool isDirectory = fileOperationsService.DirectoryExists(request.SourcePath);
            bool isFile = fileOperationsService.FileExists(request.SourcePath);

            if (!isDirectory && !isFile)
            {
                return Result<FileProcessingResult>.Failure("Source path does not exist.");
            }

            IEncryptionService encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);

            if (isFile)
            {
                // Ensure destination directory exists for single-file processing
                string? destDir = fileOperationsService.GetDirectoryName(request.DestinationPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    await fileOperationsService.CreateDirectoryAsync(destDir, cancellationToken);
                }

                // Process single file
                try
                {
                    bool result = await ProcessSingleFile(
                        encryptionService,
                        request,
                        request.SourcePath,
                        request.DestinationPath,
                        cancellationToken);

                    // Report progress for single file
                    long fileSize = fileOperationsService.GetFileSize(request.SourcePath);
                    request.Progress?.Report(new FileProcessingStatus(1, 1, fileSize, fileSize, stopwatch.Elapsed));

                    stopwatch.Stop();
                    return Result<FileProcessingResult>.Success(CreateProcessingResult(
                        result, stopwatch.Elapsed, fileSize, result ? 1 : 0, 1, errors));
                }
                catch (EncryptionException ex)
                {
                    stopwatch.Stop();
                    return Result<FileProcessingResult>.Failure(ex.Message);
                }
            }
            else
            {
                // Process directory
                (bool canProceed, IEnumerable<string> validationErrors) = await fileProcessingDomainService
                    .ValidateProcessingOperationAsync(request.SourcePath, request.DestinationPath, cancellationToken);

                if (!canProceed)
                {
                    return Result<FileProcessingResult>.Failure(validationErrors.ToArray());
                }

                string[] files = await fileOperationsService.GetFilesAsync(request.SourcePath, "*.*", cancellationToken);
                if (files.Length == 0)
                {
                    return Result<FileProcessingResult>.Success(CreateProcessingResult(
                        false, stopwatch.Elapsed, 0, 0, 0, ["No files found in the source directory."]));
                }

                long totalBytes = files.Sum(fileOperationsService.GetFileSize);
                long processedBytes = 0;
                int processedFiles = 0;

                // Report initial status
                request.Progress?.Report(new FileProcessingStatus(0, files.Length, 0, totalBytes, TimeSpan.Zero));

                await fileOperationsService.CreateDirectoryAsync(request.DestinationPath, cancellationToken);

                for (int i = 0; i < files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string file = files[i];
                    string relativePath = fileOperationsService.GetRelativePath(request.SourcePath, file);

                    string destinationFilePath = request.EncryptOperation == EncryptOperation.Encrypt
                        ? fileOperationsService.CombinePath(request.DestinationPath, relativePath + ".encrypted")
                        : fileOperationsService.CombinePath(request.DestinationPath, relativePath.Replace(".encrypted", ""));

                    string? destinationDir = fileOperationsService.GetDirectoryName(destinationFilePath);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        await fileOperationsService.CreateDirectoryAsync(destinationDir, cancellationToken);
                    }

                    try
                    {
                        bool operationResult = await ProcessSingleFile(
                            encryptionService,
                            request,
                            file,
                            destinationFilePath,
                            cancellationToken);

                        if (operationResult)
                        {
                            processedFiles++;
                        }
                    }
                    catch (EncryptionAccessDeniedException ex)
                    {
                        // Access denied - critical error, stop processing
                        stopwatch.Stop();
                        return Result<FileProcessingResult>.Failure(
                            $"Operation stopped due to access denied error: {ex.Message}");
                    }
                    catch (EncryptionInsufficientSpaceException ex)
                    {
                        // Insufficient space - critical error, stop processing
                        stopwatch.Stop();
                        return Result<FileProcessingResult>.Failure(
                            $"Operation stopped due to insufficient disk space: {ex.Message}");
                    }
                    catch (EncryptionInvalidPasswordException ex)
                    {
                        // Wrong password - critical error for decryption, stop processing
                        stopwatch.Stop();
                        return Result<FileProcessingResult>.Failure(
                            $"Operation stopped due to invalid password: {ex.Message}");
                    }
                    catch (EncryptionKeyDerivationException ex)
                    {
                        // Key derivation failure - critical error, stop processing
                        stopwatch.Stop();
                        return Result<FileProcessingResult>.Failure(
                            $"Operation stopped due to key derivation error: {ex.Message}");
                    }
                    catch (EncryptionFileNotFoundException ex)
                    {
                        // File not found - might be non-critical, log and continue
                        errors.Add($"File not found (skipped): {file} - {ex.Message}");
                    }
                    catch (EncryptionCorruptedFileException ex)
                    {
                        // Corrupted file - might be non-critical for individual files, log and continue
                        errors.Add($"Corrupted file (skipped): {file} - {ex.Message}");
                    }
                    catch (EncryptionCipherException ex)
                    {
                        // Cipher error - could be critical or non-critical depending on context
                        // For now, treat as non-critical and continue
                        errors.Add($"Cipher error for file: {file} - {ex.Message}");
                    }
                    catch (EncryptionException ex)
                    {
                        // Other encryption exceptions - treat as non-critical
                        errors.Add($"Encryption error for file: {file} - {ex.Message}");
                    }

                    long fileSize = 0;
                    try
                    {
                        fileSize = fileOperationsService.GetFileSize(file);
                    }
                    catch
                    {
                        // If we can't get file size, continue with 0
                    }

                    processedBytes += fileSize;

                    // Report progress
                    request.Progress?.Report(new FileProcessingStatus(
                        i + 1, files.Length, processedBytes, totalBytes, stopwatch.Elapsed));
                }

                stopwatch.Stop();

                // Determine if operation was successful
                bool isSuccess = errors.Count == 0 && processedFiles == files.Length;
                bool isPartialSuccess = processedFiles > 0 && processedFiles < files.Length;

                if (errors.Count > 0 && processedFiles == 0)
                {
                    // No files processed successfully, treat as failure
                    return Result<FileProcessingResult>.Failure(
                        $"Failed to process any files. Errors: {string.Join("; ", errors)}");
                }

                return Result<FileProcessingResult>.Success(CreateProcessingResult(
                    isSuccess,
                    stopwatch.Elapsed,
                    totalBytes,
                    processedFiles,
                    files.Length,
                    errors));
            }
        }
        catch (OperationCanceledException)
        {
            return Result<FileProcessingResult>.Success(CreateProcessingResult(
                false, TimeSpan.Zero, 0, 0, 0, ["Operation was cancelled."]));
        }
        catch (EncryptionException ex)
        {
            return Result<FileProcessingResult>.Failure($"Encryption error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<FileProcessingResult>.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    private static Task<bool> ProcessSingleFile(
        IEncryptionService encryptionService,
        ProcessFileCommand request,
        string sourceFile,
        string destinationFile,
        CancellationToken cancellationToken)
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