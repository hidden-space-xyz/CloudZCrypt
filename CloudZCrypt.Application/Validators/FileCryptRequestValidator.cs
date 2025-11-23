using CloudZCrypt.Application.Helpers;
using CloudZCrypt.Application.Validators.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Utilities;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Application.Validators;

internal sealed class FileCryptRequestValidator(
    IFileOperationsService fileOperations,
    ISystemStorageService systemStorage,
    IPasswordService passwordService
) : IFileProcessingRequestValidator
{
    public async Task<IReadOnlyList<string>> AnalyzeErrorsAsync(
        FileCryptRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> errors = [];

        string? sourcePath = PathNormalizationHelper.TryNormalize(
            request.SourcePath,
            out string? sourceNormalizeError
        );
        string? destinationPath = PathNormalizationHelper.TryNormalize(
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
            { /* ignore */
            }
        }

        return errors;
    }

    public async Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileCryptRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> warnings = [];

        string? sourcePath = PathNormalizationHelper.TryNormalize(request.SourcePath, out _);
        string? destinationPath = PathNormalizationHelper.TryNormalize(
            request.DestinationPath,
            out _
        );
        if (sourcePath is null || destinationPath is null)
        {
            return warnings;
        }

        try
        {
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
                            $"Low disk space: Available {ByteSizeFormatter.Format(available)}, estimated need {ByteSizeFormatter.Format(requiredSpace)}"
                        );
                    }
                }
            }

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

            Domain.ValueObjects.Password.PasswordStrengthAnalysis strength =
                passwordService.AnalyzePasswordStrength(request.Password);
            if (strength.Score < 60)
            {
                warnings.Add(
                    "Password strength is below recommended level. Consider using a stronger password."
                );
            }
        }
        catch
        { /* ignore */
        }

        return warnings;
    }
}
