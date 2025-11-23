using CloudZCrypt.Application.Helpers;
using CloudZCrypt.Application.Orchestrators.Interfaces;
using CloudZCrypt.Application.Validators.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.Domain.ValueObjects.Manifest;
using System.Diagnostics;

namespace CloudZCrypt.Application.Orchestrators;

internal sealed class FileCryptOrchestrator(
    IEncryptionServiceFactory encryptionServiceFactory,
    INameObfuscationServiceFactory nameObfuscationServiceFactory,
    IFileProcessingRequestValidator fileProcessingRequestValidator,
    IFileOperationsService fileOperations,
    IManifestService manifestService
) : IFileCryptOrchestrator
{
    private static string AppFileExtension => ".czc";
    private static string ManifestFileName => "manifest" + AppFileExtension;

    public async Task<Result<FileCryptResult>> ExecuteAsync(
        FileCryptRequest request,
        IProgress<FileCryptStatus> progress,
        CancellationToken cancellationToken = default
    )
    {
        // First run validations
        IReadOnlyList<string> errors = await fileProcessingRequestValidator.AnalyzeErrorsAsync(
            request,
            cancellationToken
        );
        if (errors.Count > 0)
        {
            return Result<FileCryptResult>.Success(
                new FileCryptResult(false, TimeSpan.Zero, 0, 0, 0, errors: errors)
            );
        }

        IReadOnlyList<string> warnings = await fileProcessingRequestValidator.AnalyzeWarningsAsync(
            request,
            cancellationToken
        );
        if (warnings.Count > 0 && !request.ProceedOnWarnings)
        {
            // Return a result carrying warnings, letting caller decide to proceed
            return Result<FileCryptResult>.Success(
                new FileCryptResult(false, TimeSpan.Zero, 0, 0, 0, warnings: warnings)
            );
        }

        // Normalize inputs using the dedicated service
        string? sourcePath = PathNormalizationHelper.TryNormalize(request.SourcePath, out _);
        string? destinationPath = PathNormalizationHelper.TryNormalize(
            request.DestinationPath,
            out _
        );
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
            throw;
        }
        catch (Exception ex)
        {
            return Result<FileCryptResult>.Failure(
                $"An unexpected error occurred: {ex.Message}"
            );
        }
    }

    private async Task<Result<FileCryptResult>> ProcessOperationAsync(
        string sourcePath,
        string destinationPath,
        FileCryptRequest request,
        IProgress<FileCryptStatus> progress,
        CancellationToken cancellationToken
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<string> errors = [];
        bool isDirectory = fileOperations.DirectoryExists(sourcePath);
        bool isFile = fileOperations.FileExists(sourcePath);
        if (!isDirectory && !isFile)
        {
            return Result<FileCryptResult>.Failure("Source path does not exist.");
        }

        IEncryptionAlgorithmStrategy encryptionService = encryptionServiceFactory.Create(
            request.EncryptionAlgorithm
        );
        INameObfuscationStrategy obfuscationService = nameObfuscationServiceFactory.Create(
            request.NameObfuscation
        );

        if (isFile)
        {
            try
            {
                // If decrypting a single file and it's the manifest, ignore it
                if (
                    request.Operation == EncryptOperation.Decrypt
                    && string.Equals(
                        Path.GetFileName(sourcePath),
                        ManifestFileName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    stopwatch.Stop();
                    return Result<FileCryptResult>.Success(
                        new FileCryptResult(
                            true,
                            stopwatch.Elapsed,
                            0,
                            0,
                            0,
                            errors: ["Manifest file ignored during decryption."]
                        )
                    );
                }

                string destFile = destinationPath;
                if (request.Operation == EncryptOperation.Encrypt)
                {
                    destFile = ApplyObfuscationToDestination(
                        destFile,
                        sourcePath,
                        obfuscationService
                    );
                }
                // Decrypt (single file): keep destination as chosen by user; no header-based rename

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
                catch
                { /* ignore */
                }

                progress?.Report(
                    new FileCryptStatus(1, 1, fileSize, fileSize, stopwatch.Elapsed)
                );
                stopwatch.Stop();
                return Result<FileCryptResult>.Success(
                    new FileCryptResult(
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
                return Result<FileCryptResult>.Failure(ex.Message);
            }
        }

        // Directory processing
        string[] files = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
        if (files.Length == 0)
        {
            stopwatch.Stop();
            return Result<FileCryptResult>.Success(
                new FileCryptResult(
                    false,
                    stopwatch.Elapsed,
                    0,
                    0,
                    0,
                    errors: ["No files found in the source directory."]
                )
            );
        }

        // Prepare optional manifest mapping for directory operations
        List<ManifestEntry> manifestEntries = [];
        Dictionary<string, string>? manifestMap = null; // obfuscated relative path -> original relative path

        // If decrypting, try to decrypt manifest first to map original names
        if (request.Operation == EncryptOperation.Decrypt)
        {
            manifestMap = await manifestService.TryReadManifestAsync(
                sourcePath,
                encryptionService,
                request,
                cancellationToken
            );
        }

        string manifestEncryptedAbsolute = Path.Combine(sourcePath, ManifestFileName);
        string manifestEncryptedRelative = fileOperations.GetRelativePath(
            sourcePath,
            manifestEncryptedAbsolute
        );

        // When decrypting, exclude the manifest from files to process
        string[] filesToProcess = files;
        if (request.Operation == EncryptOperation.Decrypt)
        {
            filesToProcess = files
                .Where(f =>
                    !string.Equals(
                        fileOperations.GetRelativePath(sourcePath, f),
                        manifestEncryptedRelative,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToArray();
        }

        if (request.Operation == EncryptOperation.Encrypt)
        {
            // ensure destination root exists
            await fileOperations.CreateDirectoryAsync(destinationPath, cancellationToken);
        }

        long totalBytes = filesToProcess.Sum(fileOperations.GetFileSize);
        long processedBytes = 0;
        int processedFiles = 0;

        // Track directory obfuscation mappings to avoid duplicates
        Dictionary<string, string> directoryObfuscationCache = new(
            StringComparer.OrdinalIgnoreCase
        );

        progress?.Report(
            new FileCryptStatus(0, filesToProcess.Length, 0, totalBytes, TimeSpan.Zero)
        );

        for (int i = 0; i < filesToProcess.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = filesToProcess[i];

            string relativePath = fileOperations.GetRelativePath(sourcePath, file);

            string destinationFilePath;

            if (request.Operation == EncryptOperation.Encrypt)
            {
                // Obfuscate the entire path (directories + filename)
                destinationFilePath = ObfuscateFullPath(
                    sourcePath,
                    file,
                    relativePath,
                    destinationPath,
                    obfuscationService,
                    directoryObfuscationCache
                );

                // Record manifest mapping: original relative path -> obfuscated relative path
                string obfuscatedRelativePath = fileOperations.GetRelativePath(
                    destinationPath,
                    destinationFilePath
                );
                manifestEntries.Add(new ManifestEntry(relativePath, obfuscatedRelativePath));
            }
            else
            {
                // Decrypt: remove the app extension first
                destinationFilePath = fileOperations.CombinePath(
                    destinationPath,
                    relativePath.Replace(AppFileExtension, "")
                );

                // Use manifest mapping exclusively when available; otherwise keep default deobfuscated path
                if (
                    manifestMap is not null
                    && manifestMap.TryGetValue(relativePath, out string? originalRelativePath)
                )
                {
                    destinationFilePath = fileOperations.CombinePath(
                        destinationPath,
                        originalRelativePath
                    );
                }
            }

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
                return Result<FileCryptResult>.Failure(
                    $"Operation stopped due to access denied error: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionInsufficientSpaceException ex)
            {
                stopwatch.Stop();
                return Result<FileCryptResult>.Failure(
                    $"Operation stopped due to insufficient disk space: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionInvalidPasswordException ex)
            {
                stopwatch.Stop();
                return Result<FileCryptResult>.Failure(
                    $"Operation stopped due to invalid password: {ex.Message}"
                );
            }
            catch (Domain.Exceptions.EncryptionKeyDerivationException ex)
            {
                stopwatch.Stop();
                return Result<FileCryptResult>.Failure(
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
            catch
            { /* ignore */
            }

            processedBytes += fileSize;
            progress?.Report(
                new FileCryptStatus(
                    i + 1,
                    filesToProcess.Length,
                    processedBytes,
                    totalBytes,
                    stopwatch.Elapsed
                )
            );
        }

        // If encrypting a directory, write and encrypt the manifest last
        if (request.Operation == EncryptOperation.Encrypt && manifestEntries.Count > 0)
        {
            IReadOnlyList<string> manifestErrors = await manifestService.TrySaveManifestAsync(
                manifestEntries,
                destinationPath,
                encryptionService,
                request,
                cancellationToken
            );
            if (manifestErrors.Count > 0)
            {
                errors.AddRange(manifestErrors);
            }
        }

        stopwatch.Stop();
        bool isSuccess = errors.Count == 0 && processedFiles == filesToProcess.Length;

        return errors.Count > 0 && processedFiles == 0
            ? Result<FileCryptResult>.Failure(
                $"Failed to process any files. Errors: {string.Join("; ", errors)}"
            )
            : Result<FileCryptResult>.Success(
                new FileCryptResult(
                    isSuccess,
                    stopwatch.Elapsed,
                    totalBytes,
                    processedFiles,
                    filesToProcess.Length,
                    errors: errors
                )
            );
    }

    private string ObfuscateFullPath(
        string sourcePath,
        string sourceFilePath,
        string relativePath,
        string destinationRoot,
        INameObfuscationStrategy obfuscationService,
        Dictionary<string, string> directoryCache
    )
    {
        // Split the relative path into segments (directories + filename)
        string[] segments = relativePath.Split(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        );

        List<string> obfuscatedSegments = new(segments.Length);
        string currentSourcePath = sourcePath;

        // Process each segment (directories first, then filename)
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            bool isLastSegment = i == segments.Length - 1;

            if (isLastSegment)
            {
                // This is the filename - add extension and obfuscate
                string filenameWithExtension = segment + AppFileExtension;
                string obfuscatedFilename = obfuscationService.ObfuscateFileName(
                    sourceFilePath,
                    filenameWithExtension
                );
                obfuscatedSegments.Add(obfuscatedFilename);
            }
            else
            {
                // This is a directory segment
                currentSourcePath = Path.Combine(currentSourcePath, segment);
                string directoryKey = fileOperations.GetRelativePath(sourcePath, currentSourcePath);

                if (!directoryCache.TryGetValue(directoryKey, out string? obfuscatedDirName))
                {
                    // Create a dummy file path for directory obfuscation
                    // Using the directory path itself as the source since directories don't have content to hash
                    obfuscatedDirName = obfuscationService.ObfuscateFileName(
                        currentSourcePath,
                        segment
                    );
                    directoryCache[directoryKey] = obfuscatedDirName;
                }

                obfuscatedSegments.Add(obfuscatedDirName);
            }
        }

        // Combine all obfuscated segments with the destination root
        return fileOperations.CombinePath([destinationRoot, .. obfuscatedSegments]);
    }

    private static Task<bool> ProcessSingleFile(
        IEncryptionAlgorithmStrategy encryptionService,
        string sourceFile,
        string destinationFile,
        FileCryptRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

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

    private string ApplyObfuscationToDestination(
        string destinationFile,
        string sourceFile,
        INameObfuscationStrategy obfuscationService
    )
    {
        string dir =
            fileOperations.GetDirectoryName(destinationFile)
            ?? Path.GetDirectoryName(destinationFile)!;
        string name = Path.GetFileName(destinationFile);
        string obfuscated = obfuscationService.ObfuscateFileName(sourceFile, name);
        return fileOperations.CombinePath(dir, obfuscated);
    }
}
