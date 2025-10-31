using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.Domain.ValueObjects.Password;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CloudZCrypt.Application.Services;

/// <summary>
/// Orchestrates end-to-end file encryption and decryption workflows by coordinating validation, 
/// pre-execution analysis, and processing operations while providing progress updates and detailed results.
/// </summary>
/// <remarks>
/// This service acts as the primary coordinator for file processing operations, managing the integration
/// between file operations, encryption services, name obfuscation strategies, and system storage validation.
/// It provides a comprehensive workflow that includes input validation, warning analysis, and execution
/// with progress reporting capabilities.
/// </remarks>
public sealed class FileProcessingOrchestrator(
    IFileOperationsService fileOperations,
    ISystemStorageService systemStorage,
    IPasswordService passwordService,
    IEncryptionServiceFactory encryptionServiceFactory,
    INameObfuscationServiceFactory nameObfuscationServiceFactory
) : IFileProcessingOrchestrator
{
    private static string AppFileExtension => ".czc";
    private static string ManifestFileName => "manifest" + AppFileExtension;


    /// <summary>
    /// Validates the supplied request for correctness and completeness before any processing occurs.
    /// </summary>
    /// <param name="request">The file processing request containing source/destination paths, credentials, and algorithm selections.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the validation operation.</param>
    /// <returns>A read-only list of validation error messages. The collection is empty when the request is valid.</returns>
    /// <remarks>
    /// Performs comprehensive validation including path normalization, file/directory existence checks,
    /// permission validation, password strength requirements, drive accessibility, and path conflict detection.
    /// This method performs only inexpensive checks and does not create or overwrite any files.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    public async Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> errors = [];

        string? sourcePath = TryNormalizePath(request.SourcePath, out string? sourceNormalizeError);
        string? destinationPath = TryNormalizePath(request.DestinationPath, out string? destinationNormalizeError);

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
        else if (!fileOperations.FileExists(sourcePath) && !fileOperations.DirectoryExists(sourcePath))
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
                    try { fileSize = fileOperations.GetFileSize(sourcePath); }
                    catch { /* ignore */ }

                    if (fileSize == 0)
                    {
                        errors.Add("The selected file is empty and cannot be processed.");
                    }
                }
                else if (fileOperations.DirectoryExists(sourcePath))
                {
                    string[] files = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
                    if (files.Length == 0)
                    {
                        errors.Add("The selected directory is empty - no files to process.");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                errors.Add("Access denied to the source path. Please check permissions or run as administrator.");
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
                        errors.Add($"The destination drive '{drive}' does not exist or is not accessible.");
                    }

                    try
                    {
                        await fileOperations.CreateDirectoryAsync(destinationDir!, cancellationToken);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errors.Add("Access denied to destination path. Please check permissions or run as administrator.");
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
        else if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            errors.Add("Password and confirmation password do not match. Please check both fields.");
        }

        if (!string.IsNullOrWhiteSpace(sourcePath) && !string.IsNullOrWhiteSpace(destinationPath))
        {
            try
            {
                if (fileOperations.FileExists(sourcePath))
                {
                    if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Source and destination files cannot be the same. Please choose a different destination.");
                    }
                }
                else if (fileOperations.DirectoryExists(sourcePath))
                {
                    if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Source and destination directories cannot be the same. Please choose a different destination.");
                    }
                    else if (destinationPath.StartsWith(sourcePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Destination directory cannot be inside the source directory. This would create a recursive operation.");
                    }
                    else if (sourcePath.StartsWith(destinationPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("Source directory cannot be inside the destination directory. Please choose a different path.");
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

    /// <summary>
    /// Analyzes the request for non-fatal conditions and potential usability warnings that may impact the user experience.
    /// </summary>
    /// <param name="request">The file processing request to inspect for warning conditions.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>A read-only list of warning messages. The collection is empty when no advisory conditions are detected.</returns>
    /// <remarks>
    /// This analysis identifies conditions such as low disk space, large batch operations, existing files that may be overwritten,
    /// and weak password strength. These warnings inform the user about suboptimal configurations without preventing execution.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    public async Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        List<string> warnings = [];

        string? sourcePath = TryNormalizePath(request.SourcePath, out _);
        string? destinationPath = TryNormalizePath(request.DestinationPath, out _);
        if (sourcePath is null || destinationPath is null)
        {
            return warnings;
        }

        try
        {
            if (fileOperations.DirectoryExists(sourcePath))
            {
                string? destinationDrive = systemStorage.GetPathRoot(destinationPath);
                if (!string.IsNullOrEmpty(destinationDrive) && systemStorage.IsDriveReady(destinationDrive))
                {
                    string[] sourceFiles = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
                    long totalSize = sourceFiles.Sum(f =>
                    {
                        try { return fileOperations.GetFileSize(f); }
                        catch { return 0; }
                    });

                    long requiredSpace = (long)(totalSize * 1.2);
                    long available = systemStorage.GetAvailableFreeSpace(destinationDrive);
                    if (available >= 0 && available < requiredSpace)
                    {
                        warnings.Add($"Low disk space: Available {FormatBytes(available)}, estimated need {FormatBytes(requiredSpace)}");
                    }
                }
            }

            if (fileOperations.DirectoryExists(sourcePath))
            {
                string[] files = await fileOperations.GetFilesAsync(sourcePath, "*.*", cancellationToken);
                int fileCount = files.Length;
                if (fileCount > 10000)
                {
                    warnings.Add($"Large operation: {fileCount:N0} files will be processed. This may take considerable time.");
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
                string[] existingFiles = await fileOperations.GetFilesAsync(destinationPath, "*.*", cancellationToken);
                if (existingFiles.Length > 0)
                {
                    hasExistingFiles = true;
                    existingFileCount = existingFiles.Length;
                }
            }

            if (hasExistingFiles)
            {
                warnings.Add($"Destination contains {existingFileCount:N0} existing file(s) that may be overwritten.");
            }

            PasswordStrengthAnalysis strength = passwordService.AnalyzePasswordStrength(request.Password);
            if (strength.Score < 60)
            {
                warnings.Add("Password strength is below recommended level. Consider using a stronger password.");
            }
        }
        catch
        {
            // ignore pre-check errors
        }

        return warnings;
    }

    /// <summary>
    /// Executes the complete file processing workflow (encryption or decryption) for the specified request.
    /// </summary>
    /// <param name="request">The file processing request that defines source and destination paths, cryptographic algorithms, and password material.</param>
    /// <param name="progress">An optional progress reporter that receives periodic status updates during execution.</param>
    /// <param name="cancellationToken">A token used to cancel the operation cooperatively.</param>
    /// <returns>
    /// A result containing a <see cref="FileProcessingResult"/> with aggregate metrics, success status, and any errors encountered.
    /// When the result indicates failure, inspect the errors collection for details.
    /// </returns>
    /// <remarks>
    /// This method handles both single file and directory processing scenarios, applying the selected encryption algorithm,
    /// key derivation function, and name obfuscation strategy. It provides progress updates and ensures efficient
    /// resource management throughout the operation.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
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

    /// <summary>
    /// Processes the core operation logic for both single file and directory scenarios.
    /// </summary>
    /// <param name="sourcePath">The normalized source file or directory path.</param>
    /// <param name="destinationPath">The normalized destination file or directory path.</param>
    /// <param name="request">The original processing request containing operation parameters.</param>
    /// <param name="progress">An optional progress reporter for status updates.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>A result containing the processing outcome with detailed metrics and any errors.</returns>
    /// <remarks>
    /// This method differentiates between file and directory processing, handles name obfuscation during encryption,
    /// attempts to restore original names during decryption strictly via the encrypted JSON manifest (no file headers).
    /// </remarks>
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
            manifestMap = await TryDecryptAndLoadManifestAsync(sourcePath, encryptionService, request, cancellationToken);
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
            try
            {
                byte[] manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifestEntries));
                // Write the encrypted manifest into the destination root so it travels with the encrypted files
                string encryptedManifestPath = fileOperations.CombinePath(destinationPath, ManifestFileName);
                bool manifestOk = await encryptionService.CreateEncryptedFileAsync(manifestBytes, encryptedManifestPath, request.Password, request.KeyDerivationAlgorithm);
                if (!manifestOk)
                {
                    errors.Add($"Failed to create encrypted manifest at '{encryptedManifestPath}'.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to write or encrypt manifest: {ex.Message}");
            }
        }

        stopwatch.Stop();
        bool isSuccess = errors.Count == 0 && processedFiles == filesToProcess.Length;

        return errors.Count > 0 && processedFiles == 0
            ? Result<FileProcessingResult>.Failure($"Failed to process any files. Errors: {string.Join("; ", errors)}")
            : Result<FileProcessingResult>.Success(new FileProcessingResult(isSuccess, stopwatch.Elapsed, totalBytes, processedFiles, filesToProcess.Length, errors));
    }

    /// <summary>
    /// Processes a single file using the specified encryption strategy and operation parameters.
    /// </summary>
    /// <param name="encryptionService">The encryption algorithm strategy to use for processing.</param>
    /// <param name="sourceFile">The source file path to process.</param>
    /// <param name="destinationFile">The destination file path for the processed output.</param>
    /// <param name="request">The processing request containing operation type and credentials.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with a result indicating success or failure.</returns>
    /// <exception cref="NotSupportedException">Thrown when an unsupported operation type is specified.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
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

    /// <summary>
    /// Applies name obfuscation to the destination file path using the specified obfuscation strategy.
    /// </summary>
    /// <param name="destinationFile">The original destination file path.</param>
    /// <param name="sourceFile">The source file path used for obfuscation context.</param>
    /// <param name="obfuscationService">The name obfuscation strategy to apply.</param>
    /// <returns>The destination file path with the obfuscated filename while preserving the directory structure.</returns>
    private string ApplyObfuscationToDestination(string destinationFile, string sourceFile, INameObfuscationStrategy obfuscationService)
    {
        string dir = fileOperations.GetDirectoryName(destinationFile) ?? Path.GetDirectoryName(destinationFile)!;
        string name = Path.GetFileName(destinationFile);
        string obfuscated = obfuscationService.ObfuscateFileName(sourceFile, name);
        return fileOperations.CombinePath(dir, obfuscated);
    }

    /// <summary>
    /// Attempts to decrypt and load the manifest stored at the directory root. Returns null if unavailable or on failure.
    /// </summary>
    private static async Task<Dictionary<string, string>?> TryDecryptAndLoadManifestAsync(
        string sourceRoot,
        IEncryptionAlgorithmStrategy encryptionService,
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            string encryptedManifestPath = Path.Combine(sourceRoot, ManifestFileName);
            if (!File.Exists(encryptedManifestPath))
            {
                return null;
            }

            string tempJsonPath = Path.Combine(Path.GetTempPath(), $"czc-manifest-{Guid.NewGuid():N}.json");
            bool ok = await encryptionService.DecryptFileAsync(encryptedManifestPath, tempJsonPath, request.Password, request.KeyDerivationAlgorithm);
            if (!ok)
            {
                try { if (File.Exists(tempJsonPath)) File.Delete(tempJsonPath); } catch { }
                return null;
            }

            try
            {
                await using FileStream fs = File.OpenRead(tempJsonPath);
                List<NameMapEntry>? entries = await JsonSerializer.DeserializeAsync<List<NameMapEntry>>(fs, cancellationToken: cancellationToken);
                Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
                if (entries is not null)
                {
                    foreach (NameMapEntry e in entries)
                    {
                        // Key is obfuscated relative path, value is original relative path
                        map[e.ObfuscatedRelativePath] = e.OriginalRelativePath;
                    }
                }
                return map;
            }
            finally
            {
                try { if (File.Exists(tempJsonPath)) File.Delete(tempJsonPath); } catch { }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to normalize and expand the specified file or directory path.
    /// </summary>
    /// <param name="rawPath">The raw path string to normalize.</param>
    /// <param name="error">When this method returns, contains the error message if normalization failed; otherwise, null.</param>
    /// <returns>The normalized full path if successful; otherwise, null.</returns>
    /// <remarks>
    /// This method expands environment variables, resolves relative paths to absolute paths, and handles various
    /// path format inconsistencies. If normalization fails due to invalid characters or malformed paths,
    /// an error message is provided through the out parameter.
    /// </remarks>
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

    /// <summary>
    /// Formats a byte count into a human-readable string with appropriate unit suffixes.
    /// </summary>
    /// <param name="bytes">The number of bytes to format.</param>
    /// <returns>A formatted string representing the byte count with appropriate units (B, KB, MB, GB, TB).</returns>
    /// <remarks>
    /// Uses base-1024 calculations for unit conversions and formats the result to one decimal place.
    /// Handles both positive and negative values, with zero bytes returning "0 B".
    /// </remarks>
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
