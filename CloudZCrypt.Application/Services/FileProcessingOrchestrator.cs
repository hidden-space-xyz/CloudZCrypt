using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.Domain.ValueObjects.Password;

namespace CloudZCrypt.Application.Services;

public sealed class FileProcessingOrchestrator(
    IFileOperationsService fileOperations,
    ISystemStorageService systemStorage,
    IPasswordService passwordService,
    IEncryptionServiceFactory encryptionServiceFactory
) : IFileProcessingOrchestrator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> errors = [];

        // Normalize inputs early
        string? sourcePath = TryNormalizePath(request.SourcePath, out string? sourceNormalizeError);
        string? destinationPath = TryNormalizePath(
            request.DestinationPath,
            out string? destinationNormalizeError
        );
        if (sourceNormalizeError is not null)
        {
            errors.Add(sourceNormalizeError);
        }

        if (destinationNormalizeError is not null)
        {
            errors.Add(destinationNormalizeError);
        }

        if (sourcePath is null || destinationPath is null)
        {
            return errors;
        }

        // Source validation
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            errors.Add("Please select a source file or directory to process.");
        }
        else if (
            !fileOperations.FileExists(sourcePath) && !fileOperations.DirectoryExists(sourcePath)
        )
        {
            errors.Add($"The selected source path does not exist: {sourcePath}");
        }
        else
        {
            try
            {
                if (fileOperations.FileExists(sourcePath))
                {
                    long fileSize = 0;
                    try
                    {
                        fileSize = fileOperations.GetFileSize(sourcePath);
                    }
                    catch
                    { /* ignore */
                    }
                    if (fileSize == 0)
                    {
                        errors.Add("The selected file is empty and cannot be processed.");
                    }
                }
                else if (fileOperations.DirectoryExists(sourcePath))
                {
                    string[] files = await fileOperations.GetFilesAsync(
                        sourcePath,
                        "*.*",
                        cancellationToken
                    );
                    if (files.Length == 0)
                    {
                        errors.Add("The selected directory is empty - no files to process.");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                errors.Add(
                    "Access denied to the source path. Please check permissions or run as administrator."
                );
            }
            catch (Exception ex)
            {
                errors.Add($"Error accessing source path: {ex.Message}");
            }
        }

        // Destination validation
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            errors.Add("Please select a destination path.");
        }
        else
        {
            try
            {
                string? destinationDir = fileOperations.FileExists(sourcePath)
                    ? fileOperations.GetDirectoryName(destinationPath)
                    : destinationPath;

                if (!string.IsNullOrEmpty(destinationDir))
                {
                    string? drive = systemStorage.GetPathRoot(destinationDir);

                    if (!string.IsNullOrEmpty(drive) && !systemStorage.IsDriveReady(drive))
                    {
                        errors.Add(
                            $"The destination drive '{drive}' does not exist or is not accessible."
                        );
                    }

                    try
                    {
                        await fileOperations.CreateDirectoryAsync(
                            destinationDir!,
                            cancellationToken
                        );
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errors.Add(
                            "Access denied to destination path. Please check permissions or run as administrator."
                        );
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Cannot write to destination path: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Invalid destination path: {ex.Message}");
            }
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Please enter a password for encryption/decryption.");
        }
        else
        {
            if (request.Password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long for security.");
            }
            if (request.Password.Length > 1000)
            {
                errors.Add("Password is too long (maximum 1000 characters).");
            }
            if (request.Password.Trim() != request.Password)
            {
                errors.Add("Password should not start or end with spaces.");
            }
        }

        // Confirm password validation
        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            errors.Add("Please confirm your password.");
        }
        else if (
            !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal)
        )
        {
            errors.Add(
                "Password and confirmation password do not match. Please check both fields."
            );
        }

        // Path conflict validation (use normalized absolute paths computed above)
        if (!string.IsNullOrWhiteSpace(sourcePath) && !string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                if (fileOperations.FileExists(sourcePath))
                {
                    if (
                        string.Equals(
                            sourcePath,
                            destinationPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        errors.Add(
                            "Source and destination files cannot be the same. Please choose a different destination."
                        );
                    }
                }
                else if (fileOperations.DirectoryExists(sourcePath))
                {
                    if (
                        string.Equals(
                            sourcePath,
                            destinationPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        errors.Add(
                            "Source and destination directories cannot be the same. Please choose a different destination."
                        );
                    }
                    else if (
                        destinationPath.StartsWith(
                            sourcePath + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        errors.Add(
                            "Destination directory cannot be inside the source directory. This would create a recursive operation."
                        );
                    }
                    else if (
                        sourcePath.StartsWith(
                            destinationPath + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        errors.Add(
                            "Source directory cannot be inside the destination directory. Please choose a different path."
                        );
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        return errors;
    }

    public async Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> warnings = [];

        // Normalize inputs for analysis
        string? sourcePath = TryNormalizePath(request.SourcePath, out _);
        string? destinationPath = TryNormalizePath(request.DestinationPath, out _);
        if (sourcePath is null || destinationPath is null)
        {
            return warnings;
        }

        try
        {
            // Disk space (directory scenario)
            if (fileOperations.DirectoryExists(sourcePath))
            {
                string? destinationDrive = systemStorage.GetPathRoot(destinationPath);
                if (
                    !string.IsNullOrEmpty(destinationDrive)
                    && systemStorage.IsDriveReady(destinationDrive)
                )
                {
                    string[] sourceFiles = await fileOperations.GetFilesAsync(
                        sourcePath,
                        "*.*",
                        cancellationToken
                    );
                    long totalSize = sourceFiles.Sum(f =>
                    {
                        try
                        {
                            return fileOperations.GetFileSize(f);
                        }
                        catch
                        {
                            return 0;
                        }
                    });

                    long requiredSpace = (long)(totalSize * 1.2);
                    long available = systemStorage.GetAvailableFreeSpace(destinationDrive);
                    if (available >= 0 && available < requiredSpace)
                    {
                        warnings.Add(
                            $"Low disk space: Available {FormatBytes(available)}, estimated need {FormatBytes(requiredSpace)}"
                        );
                    }
                }
            }

            // Large operation warning
            if (fileOperations.DirectoryExists(sourcePath))
            {
                string[] files = await fileOperations.GetFilesAsync(
                    sourcePath,
                    "*.*",
                    cancellationToken
                );
                int fileCount = files.Length;
                if (fileCount > 10000)
                {
                    warnings.Add(
                        $"Large operation: {fileCount:N0} files will be processed. This may take considerable time."
                    );
                }
                else if (fileCount > 1000)
                {
                    warnings.Add($"Medium operation: {fileCount:N0} files will be processed.");
                }
            }

            // Overwrite checks
            bool hasExistingFiles = false;
            int existingFileCount = 0;

            if (fileOperations.FileExists(sourcePath) && fileOperations.FileExists(destinationPath))
            {
                hasExistingFiles = true;
                existingFileCount = 1;
            }
            else if (fileOperations.DirectoryExists(destinationPath))
            {
                string[] existingFiles = await fileOperations.GetFilesAsync(
                    destinationPath,
                    "*.*",
                    cancellationToken
                );
                if (existingFiles.Length > 0)
                {
                    hasExistingFiles = true;
                    existingFileCount = existingFiles.Length;
                }
            }

            if (hasExistingFiles)
            {
                warnings.Add(
                    $"Destination contains {existingFileCount:N0} existing file(s) that may be overwritten."
                );
            }

            // Password strength warning via domain service
            PasswordStrengthAnalysis strength = passwordService.AnalyzePasswordStrength(
                request.Password
            );
            if (strength.Score < 60)
            {
                warnings.Add(
                    "Password strength is below recommended level. Consider using a stronger password."
                );
            }
        }
        catch
        {
            // ignore pre-check errors
        }

        return warnings;
    }

    public async Task<Result<FileProcessingResult>> ExecuteAsync(
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken = default
    )
    {
        // Normalize inputs
        string? sourcePath = TryNormalizePath(request.SourcePath, out _);
        string? destinationPath = TryNormalizePath(request.DestinationPath, out _);
        sourcePath ??= request.SourcePath;
        destinationPath ??= request.DestinationPath;

        // Ensure destination exists
        if (fileOperations.DirectoryExists(sourcePath))
        {
            await fileOperations.CreateDirectoryAsync(destinationPath, cancellationToken);
        }
        else
        {
            string? destDir = fileOperations.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                await fileOperations.CreateDirectoryAsync(destDir, cancellationToken);
            }
        }

        try
        {
            return await ProcessOperationAsync(
                sourcePath,
                destinationPath,
                request,
                progress,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            return Result<FileProcessingResult>.Success(
                new FileProcessingResult(
                    false,
                    TimeSpan.Zero,
                    0,
                    0,
                    0,
                    ["Operation was cancelled."]
                )
            );
        }
        catch (Exception ex)
        {
            return Result<FileProcessingResult>.Failure(
                $"An unexpected error occurred: {ex.Message}"
            );
        }
    }

    private async Task<Result<FileProcessingResult>> ProcessOperationAsync(
        string sourcePath,
        string destinationPath,
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken
    )
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        List<string> errors = [];
        bool isDirectory = fileOperations.DirectoryExists(sourcePath);
        bool isFile = fileOperations.FileExists(sourcePath);
        if (!isDirectory && !isFile)
        {
            return Result<FileProcessingResult>.Failure("Source path does not exist.");
        }

        IEncryptionAlgorithmStrategy encryptionService = encryptionServiceFactory.Create(
            request.EncryptionAlgorithm
        );
        if (isFile)
        {
            try
            {
                string destFile = destinationPath;
                bool result = await ProcessSingleFile(
                    encryptionService,
                    sourcePath,
                    destFile,
                    request,
                    cancellationToken
                );
                long fileSize = 0;
                try
                {
                    fileSize = fileOperations.GetFileSize(sourcePath);
                }
                catch { }
                progress?.Report(
                    new FileProcessingStatus(1, 1, fileSize, fileSize, stopwatch.Elapsed)
                );
                stopwatch.Stop();
                return Result<FileProcessingResult>.Success(
                    new FileProcessingResult(
                        result,
                        stopwatch.Elapsed,
                        fileSize,
                        result ? 1 : 0,
                        1,
                        errors
                    )
                );
            }
            catch (Domain.Exceptions.EncryptionException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(ex.Message);
            }
        }
        // Directory
        string[] files = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
        if (files.Length == 0)
        {
            stopwatch.Stop();
            return Result<FileProcessingResult>.Success(
                new FileProcessingResult(
                    false,
                    stopwatch.Elapsed,
                    0,
                    0,
                    0,
                    ["No files found in the source directory."]
                )
            );
        }
        long totalBytes = files.Sum(fileOperations.GetFileSize);
        long processedBytes = 0;
        int processedFiles = 0;
        progress?.Report(new FileProcessingStatus(0, files.Length, 0, totalBytes, TimeSpan.Zero));
        await fileOperations.CreateDirectoryAsync(destinationPath, cancellationToken);
        for (int i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = files[i];
            string relativePath = fileOperations.GetRelativePath(sourcePath, file);
            string destinationFilePath =
                request.Operation == EncryptOperation.Encrypt
                    ? fileOperations.CombinePath(destinationPath, relativePath + ".encrypted")
                    : fileOperations.CombinePath(
                        destinationPath,
                        relativePath.Replace(".encrypted", "")
                    );
            string? destDir = fileOperations.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(destDir))
            {
                await fileOperations.CreateDirectoryAsync(destDir, cancellationToken);
            }

            try
            {
                bool operationResult = await ProcessSingleFile(
                    encryptionService,
                    file,
                    destinationFilePath,
                    request,
                    cancellationToken
                );
                if (operationResult)
                {
                    processedFiles++;
                }
            }
            catch (Domain.Exceptions.EncryptionAccessDeniedException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(
                    $"Operation stopped due to access denied error: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionInsufficientSpaceException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(
                    $"Operation stopped due to insufficient disk space: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionInvalidPasswordException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(
                    $"Operation stopped due to invalid password: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionKeyDerivationException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(
                    $"Operation stopped due to key derivation error: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionFileNotFoundException ex)
            {
                errors.Add($"File not found (skipped): {file} - {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionCorruptedFileException ex)
            {
                errors.Add($"Corrupted file (skipped): {file} - {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionCipherException ex)
            {
                errors.Add($"Cipher error for file: {file} - {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionException ex)
            {
                errors.Add($"Encryption error for file: {file} - {ex.Message}");
            }
            long fileSize = 0;
            try
            {
                fileSize = fileOperations.GetFileSize(file);
            }
            catch { }
            processedBytes += fileSize;
            progress?.Report(
                new FileProcessingStatus(
                    i + 1,
                    files.Length,
                    processedBytes,
                    totalBytes,
                    stopwatch.Elapsed
                )
            );
        }
        stopwatch.Stop();
        bool isSuccess = errors.Count == 0 && processedFiles == files.Length;
        return errors.Count > 0 && processedFiles == 0
            ? Result<FileProcessingResult>.Failure(
                $"Failed to process any files. Errors: {string.Join("; ", errors)}"
            )
            : Result<FileProcessingResult>.Success(
                new FileProcessingResult(
                    isSuccess,
                    stopwatch.Elapsed,
                    totalBytes,
                    processedFiles,
                    files.Length,
                    errors
                )
            );
    }

    private Task<bool> ProcessSingleFile(
        IEncryptionAlgorithmStrategy encryptionService,
        string sourceFile,
        string destinationFile,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    )
    {
        return request.Operation switch
        {
            EncryptOperation.Encrypt => encryptionService.EncryptFileAsync(
                sourceFile,
                destinationFile,
                request.Password,
                request.KeyDerivationAlgorithm
            ),
            EncryptOperation.Decrypt => encryptionService.DecryptFileAsync(
                sourceFile,
                destinationFile,
                request.Password,
                request.KeyDerivationAlgorithm
            ),
            _ => throw new NotSupportedException($"Unsupported operation: {request.Operation}"),
        };
    }

    private static string? TryNormalizePath(string rawPath, out string? error)
    {
        error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }
            string expanded = Environment.ExpandEnvironmentVariables(rawPath.Trim());
            string full = Path.GetFullPath(expanded);
            return full;
        }
        catch (Exception ex)
        {
            error = $"Invalid path: {ex.Message}";
            return null;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0)
        {
            return "0 B";
        }
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = Math.Abs(bytes);
        int suffixIndex = 0;
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}
