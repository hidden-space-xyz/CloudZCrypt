using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Utilities;

namespace CloudZCrypt.Application.Services;

internal sealed class FileProcessingWarningAnalyzer(
    IFileOperationsService fileOperations,
    ISystemStorageService systemStorage,
    IPasswordService passwordService,
    IPathNormalizer pathNormalizer
) : IFileProcessingWarningAnalyzer
{
    public async Task<IReadOnlyList<string>> AnalyzeAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> warnings = [];

        string? sourcePath = pathNormalizer.TryNormalize(request.SourcePath, out _);
        string? destinationPath = pathNormalizer.TryNormalize(request.DestinationPath, out _);
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

            Domain.ValueObjects.Password.PasswordStrengthAnalysis strength = passwordService.AnalyzePasswordStrength(request.Password);
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
}
