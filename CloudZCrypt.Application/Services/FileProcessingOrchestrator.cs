using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using System.Diagnostics;

namespace CloudZCrypt.Application.Services;


internal sealed class FileProcessingOrchestrator(
    IFileOperationsService fileOperations,
    IEncryptionServiceFactory encryptionServiceFactory,
    INameObfuscationServiceFactory nameObfuscationServiceFactory,
    IFileProcessingRequestValidator requestValidator,
    IFileProcessingWarningAnalyzer warningAnalyzer,
    IPathNormalizer pathNormalizer,
    IManifestService manifestService
) : IFileProcessingOrchestrator
{
    private static string AppFileExtension => ".czc";
    private static string ManifestFileName => "manifest" + AppFileExtension;

    
    public Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    ) => requestValidator.ValidateAsync(request, cancellationToken);

    
    public Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    ) => warningAnalyzer.AnalyzeAsync(request, cancellationToken);

    
    public async Task<Result<FileProcessingResult>> ExecuteAsync(
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken = default
    )
    {
        // Normalize inputs using the dedicated service
        string? sourcePath = pathNormalizer.TryNormalize(request.SourcePath, out _);
        string? destinationPath = pathNormalizer.TryNormalize(request.DestinationPath, out _);
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
            return await ProcessOperationAsync(sourcePath, destinationPath, request, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result<FileProcessingResult>.Success(new FileProcessingResult(false, TimeSpan.Zero, 0, 0, 0, ["Operation was cancelled."]));
        }
        catch (Exception ex)
        {
            return Result<FileProcessingResult>.Failure($"An unexpected error occurred: {ex.Message}");
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
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<string> errors = [];
        bool isDirectory = fileOperations.DirectoryExists(sourcePath);
        bool isFile = fileOperations.FileExists(sourcePath);
        if (!isDirectory && !isFile)
        {
            return Result<FileProcessingResult>.Failure("Source path does not exist.");
        }

        IEncryptionAlgorithmStrategy encryptionService = encryptionServiceFactory.Create(request.EncryptionAlgorithm);
        INameObfuscationStrategy obfuscationService = nameObfuscationServiceFactory.Create(request.NameObfuscation);

        if (isFile)
        {
            try
            {
                // If decrypting a single file and it's the manifest, ignore it
                if (request.Operation == EncryptOperation.Decrypt && string.Equals(Path.GetFileName(sourcePath), ManifestFileName, StringComparison.OrdinalIgnoreCase))
                {
                    stopwatch.Stop();
                    return Result<FileProcessingResult>.Success(new FileProcessingResult(true, stopwatch.Elapsed, 0, 0, 0, ["Manifest file ignored during decryption."]));
                }

                string destFile = destinationPath;
                if (request.Operation == EncryptOperation.Encrypt)
                {
                    destFile = ApplyObfuscationToDestination(destFile, sourcePath, obfuscationService);
                }
                // Decrypt (single file): keep destination as chosen by user; no header-based rename

                bool result = await ProcessSingleFile(encryptionService, sourcePath, destFile, request, cancellationToken);
                long fileSize = 0;
                try { fileSize = fileOperations.GetFileSize(sourcePath); }
                catch { /* ignore */ }

                progress?.Report(new FileProcessingStatus(1, 1, fileSize, fileSize, stopwatch.Elapsed));
                stopwatch.Stop();
                return Result<FileProcessingResult>.Success(new FileProcessingResult(result, stopwatch.Elapsed, fileSize, result ? 1 : 0, 1, errors));
            }
            catch (Domain.Exceptions.EncryptionException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure(ex.Message);
            }
        }

        // Directory processing
        string[] files = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
        if (files.Length == 0)
        {
            stopwatch.Stop();
            return Result<FileProcessingResult>.Success(new FileProcessingResult(false, stopwatch.Elapsed, 0, 0, 0, ["No files found in the source directory."]));
        }

        // Prepare optional manifest mapping for directory operations
        List<NameMapEntry> manifestEntries = [];
        Dictionary<string, string>? manifestMap = null; // obfuscated relative path -> original relative path

        // If decrypting, try to decrypt manifest first to map original names
        if (request.Operation == EncryptOperation.Decrypt)
        {
            manifestMap = await manifestService.TryReadMapAsync(sourcePath, encryptionService, request, cancellationToken);
        }

        string manifestEncryptedAbsolute = Path.Combine(sourcePath, ManifestFileName);
        string manifestEncryptedRelative = fileOperations.GetRelativePath(sourcePath, manifestEncryptedAbsolute);

        // When decrypting, exclude the manifest from files to process
        string[] filesToProcess = files;
        if (request.Operation == EncryptOperation.Decrypt)
        {
            filesToProcess = files
                .Where(f => !string.Equals(fileOperations.GetRelativePath(sourcePath, f), manifestEncryptedRelative, StringComparison.OrdinalIgnoreCase))
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

        progress?.Report(new FileProcessingStatus(0, filesToProcess.Length, 0, totalBytes, TimeSpan.Zero));

        for (int i = 0; i < filesToProcess.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string file = filesToProcess[i];

            string relativePath = fileOperations.GetRelativePath(sourcePath, file);

            string destinationFilePath = request.Operation == EncryptOperation.Encrypt
                ? fileOperations.CombinePath(destinationPath, relativePath + AppFileExtension)
                : fileOperations.CombinePath(destinationPath, relativePath.Replace(AppFileExtension, ""));

            if (request.Operation == EncryptOperation.Encrypt)
            {
                string dir = fileOperations.GetDirectoryName(destinationFilePath) ?? destinationPath;
                string name = Path.GetFileName(destinationFilePath);
                string obfuscatedName = obfuscationService.ObfuscateFileName(file, name);
                destinationFilePath = fileOperations.CombinePath(dir, obfuscatedName);

                // Record manifest mapping: original relative path -> obfuscated relative path (under destination root)
                string obfuscatedRelativePath = fileOperations.GetRelativePath(destinationPath, destinationFilePath);
                manifestEntries.Add(new NameMapEntry(relativePath, obfuscatedRelativePath));
            }
            else
            {
                // Use manifest mapping exclusively when available; otherwise keep default deobfuscated path
                if (manifestMap is not null && manifestMap.TryGetValue(relativePath, out string originalRelativePath))
                {
                    destinationFilePath = fileOperations.CombinePath(destinationPath, originalRelativePath);
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
                return Result<FileProcessingResult>.Failure($"Operation stopped due to access denied error: {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionInsufficientSpaceException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure($"Operation stopped due to insufficient disk space: {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionInvalidPasswordException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure($"Operation stopped due to invalid password: {ex.Message}");
            }
            catch (Domain.Exceptions.EncryptionKeyDerivationException ex)
            {
                stopwatch.Stop();
                return Result<FileProcessingResult>.Failure($"Operation stopped due to key derivation error: {ex.Message}");
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
            try { fileSize = fileOperations.GetFileSize(file); }
            catch { /* ignore */ }

            processedBytes += fileSize;
            progress?.Report(new FileProcessingStatus(i + 1, filesToProcess.Length, processedBytes, totalBytes, stopwatch.Elapsed));
        }

        // If encrypting a directory, write and encrypt the manifest last
        if (request.Operation == EncryptOperation.Encrypt && manifestEntries.Count > 0)
        {
            var manifestErrors = await manifestService.WriteAsync(manifestEntries, destinationPath, encryptionService, request, cancellationToken);
            if (manifestErrors.Count > 0)
            {
                errors.AddRange(manifestErrors);
            }
        }

        stopwatch.Stop();
        bool isSuccess = errors.Count == 0 && processedFiles == filesToProcess.Length;

        return errors.Count > 0 && processedFiles == 0
            ? Result<FileProcessingResult>.Failure($"Failed to process any files. Errors: {string.Join("; ", errors)}")
            : Result<FileProcessingResult>.Success(new FileProcessingResult(isSuccess, stopwatch.Elapsed, totalBytes, processedFiles, filesToProcess.Length, errors));
    }

    private static Task<bool> ProcessSingleFile(
        IEncryptionAlgorithmStrategy encryptionService,
        string sourceFile,
        string destinationFile,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return request.Operation switch
        {
            EncryptOperation.Encrypt => encryptionService.EncryptFileAsync(sourceFile, destinationFile, request.Password, request.KeyDerivationAlgorithm),
            EncryptOperation.Decrypt => encryptionService.DecryptFileAsync(sourceFile, destinationFile, request.Password, request.KeyDerivationAlgorithm),
            _ => throw new NotSupportedException($"Unsupported operation: {request.Operation}"),
        };
    }

    private string ApplyObfuscationToDestination(string destinationFile, string sourceFile, INameObfuscationStrategy obfuscationService)
    {
        string dir = fileOperations.GetDirectoryName(destinationFile) ?? Path.GetDirectoryName(destinationFile)!;
        string name = Path.GetFileName(destinationFile);
        string obfuscated = obfuscationService.ObfuscateFileName(sourceFile, name);
        return fileOperations.CombinePath(dir, obfuscated);
    }
}
