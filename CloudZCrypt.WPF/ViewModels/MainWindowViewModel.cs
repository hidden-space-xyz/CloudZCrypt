using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Commands;
using CloudZCrypt.WPF.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CloudZCrypt.WPF.ViewModels;

/// <summary>
/// Provides the primary view model for the application's main window, orchestrating user interactions
/// for selecting sources/destinations, configuring cryptographic options, generating passwords, and
/// executing encryption/decryption operations with real-time progress reporting and validation.
/// </summary>
/// <remarks>
/// This view model coordinates dialog interactions, password strength analysis, file processing workflow
/// execution, and UI state management (enabling/disabling controls, displaying progress, and surfacing
/// warnings or errors). It exposes commands bound from the UI to encapsulate user actions while keeping
/// business and infrastructure concerns decoupled.
/// </remarks>
public class MainWindowViewModel : ObservableObjectBase
{
    private readonly IDialogService dialogService;
    private readonly IFileProcessingOrchestrator orchestrator;
    private CancellationTokenSource? cancellationTokenSource;

    private string sourceFilePath = string.Empty;
    private string destinationPath = string.Empty;
    private string password = string.Empty;
    private string confirmPassword = string.Empty;
    private bool isPasswordVisible;
    private bool isConfirmPasswordVisible;
    private EncryptionAlgorithm selectedEncryptionAlgorithm;
    private KeyDerivationAlgorithm selectedKeyDerivationAlgorithm;
    private NameObfuscationMode selectedNameObfuscationMode = NameObfuscationMode.None;
    private bool isProcessing;
    private double progressValue;
    private string progressText = string.Empty;
    private bool areControlsEnabled = true;

    /// <summary>
    /// Exposes the password service for XAML bindings (behaviors, etc.).
    /// </summary>
    public IPasswordService PasswordService { get; }

    /// <summary>
    /// Gets or sets the absolute or relative path of the source file or directory to encrypt or decrypt.
    /// </summary>
    public string SourceFilePath
    {
        get => sourceFilePath;
        set
        {
            if (SetProperty(ref sourceFilePath, value))
            {
                RefreshProcessCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets the target destination path for the processed output file or directory.
    /// </summary>
    public string DestinationPath
    {
        get => destinationPath;
        set
        {
            if (SetProperty(ref destinationPath, value))
            {
                RefreshProcessCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets the user-supplied password used to derive encryption/decryption keys.
    /// </summary>
    public string Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                RefreshProcessCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets the confirmation password value, validated against <see cref="Password"/>.
    /// </summary>
    public string ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            if (SetProperty(ref confirmPassword, value))
            {
                RefreshProcessCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the primary password field is displayed in plain text.
    /// </summary>
    public bool IsPasswordVisible
    {
        get => isPasswordVisible;
        set => SetProperty(ref isPasswordVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the confirmation password field is displayed in plain text.
    /// </summary>
    public bool IsConfirmPasswordVisible
    {
        get => isConfirmPasswordVisible;
        set => SetProperty(ref isConfirmPasswordVisible, value);
    }

    /// <summary>
    /// Gets or sets the currently selected encryption algorithm.
    /// Changing the value updates the related metadata binding.
    /// </summary>
    public EncryptionAlgorithm SelectedEncryptionAlgorithm
    {
        get => selectedEncryptionAlgorithm;
        set
        {
            if (SetProperty(ref selectedEncryptionAlgorithm, value))
            {
                OnPropertyChanged(nameof(SelectedEncryptionAlgorithmInfo));
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently selected key derivation algorithm used for password-based key generation.
    /// </summary>
    public KeyDerivationAlgorithm SelectedKeyDerivationAlgorithm
    {
        get => selectedKeyDerivationAlgorithm;
        set
        {
            if (SetProperty(ref selectedKeyDerivationAlgorithm, value))
            {
                OnPropertyChanged(nameof(SelectedKeyDerivationAlgorithmInfo));
            }
        }
    }

    /// <summary>
    /// Gets or sets the currently selected name obfuscation mode.
    /// Changing the value updates the related metadata binding.
    /// </summary>
    public NameObfuscationMode SelectedNameObfuscationMode
    {
        get => selectedNameObfuscationMode;
        set
        {
            if (SetProperty(ref selectedNameObfuscationMode, value))
            {
                OnPropertyChanged(nameof(SelectedNameObfuscationModeInfo));
            }
        }
    }

    /// <summary>
    /// Gets the metadata view model describing the selected encryption algorithm or null if not resolved.
    /// </summary>
    public IEncryptionAlgorithmStrategy? SelectedEncryptionAlgorithmInfo =>
        AvailableEncryptionAlgorithms.FirstOrDefault(a => a.Id == selectedEncryptionAlgorithm);

    /// <summary>
    /// Gets the metadata view model describing the selected key derivation algorithm or null if not resolved.
    /// </summary>
    public IKeyDerivationAlgorithmStrategy? SelectedKeyDerivationAlgorithmInfo =>
        AvailableKeyDerivationAlgorithms.FirstOrDefault(a => a.Id == selectedKeyDerivationAlgorithm);

    /// <summary>
    /// Gets the metadata view model describing the selected name obfuscation mode or null if not resolved.
    /// </summary>
    public INameObfuscationStrategy? SelectedNameObfuscationModeInfo =>
        AvailableNameObfuscationModes.FirstOrDefault(a => a.Id == selectedNameObfuscationMode);

    /// <summary>
    /// Gets or sets a value indicating whether a file processing operation is currently running.
    /// Affects UI control enablement and command availability.
    /// </summary>
    public bool IsProcessing
    {
        get => isProcessing;
        set
        {
            if (SetProperty(ref isProcessing, value))
            {
                UpdateControlState();
                RefreshProcessCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current progress percentage (0–100) for the active processing operation.
    /// </summary>
    public double ProgressValue
    {
        get => progressValue;
        set => SetProperty(ref progressValue, value);
    }

    /// <summary>
    /// Gets or sets the formatted progress status text displayed to the user.
    /// </summary>
    public string ProgressText
    {
        get => progressText;
        set => SetProperty(ref progressText, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether primary interactive controls are currently enabled.
    /// </summary>
    public bool AreControlsEnabled
    {
        get => areControlsEnabled;
        set => SetProperty(ref areControlsEnabled, value);
    }

    /// <summary>
    /// Gets the collection of available encryption algorithm options displayed to the user.
    /// </summary>
    public ObservableCollection<IEncryptionAlgorithmStrategy> AvailableEncryptionAlgorithms { get; }

    /// <summary>
    /// Gets the collection of available key derivation algorithm options displayed to the user.
    /// </summary>
    public ObservableCollection<IKeyDerivationAlgorithmStrategy> AvailableKeyDerivationAlgorithms { get; }

    /// <summary>
    /// Gets the collection of available name obfuscation mode options displayed to the user.
    /// </summary>
    public ObservableCollection<INameObfuscationStrategy> AvailableNameObfuscationModes { get; }

    /// <summary>
    /// Gets the command that generates a new strong password and optionally copies it to the clipboard.
    /// </summary>
    public ICommand GenerateStrongPasswordCommand { get; }

    /// <summary>
    /// Gets the command that opens a file selection dialog for choosing a single source file.
    /// </summary>
    public ICommand SelectSourceFileCommand { get; }

    /// <summary>
    /// Gets the command that opens a folder selection dialog for choosing a source directory.
    /// </summary>
    public ICommand SelectSourceDirectoryCommand { get; }

    /// <summary>
    /// Gets the command that opens a dialog for selecting or specifying the destination path.
    /// </summary>
    public ICommand SelectDestinationPathCommand { get; }

    /// <summary>
    /// Gets the command that toggles password visibility for the primary password field.
    /// </summary>
    public ICommand TogglePasswordVisibilityCommand { get; }

    /// <summary>
    /// Gets the command that toggles password visibility for the confirmation password field.
    /// </summary>
    public ICommand ToggleConfirmPasswordVisibilityCommand { get; }

    /// <summary>
    /// Gets the command that initiates encryption of the selected file or directory.
    /// </summary>
    public ICommand EncryptFileCommand { get; }

    /// <summary>
    /// Gets the command that initiates decryption of the selected file or directory.
    /// </summary>
    public ICommand DecryptFileCommand { get; }

    /// <summary>
    /// Gets the command that cancels the ongoing processing operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dialogService">The dialog service used for user interaction (messages, confirmations, file pickers).</param>
    /// <param name="orchestrator">The orchestrator responsible for validating and executing file processing workflows.</param>
    /// <param name="passwordService">The password service used for analysis and generation operations.</param>
    /// <param name="encryptionStrategies">The collection of available encryption algorithm strategies.</param>
    /// <param name="keyDerivationStrategies">The collection of available key derivation algorithm strategies.</param>
    /// <param name="nameObfuscationStrategies">The collection of available name obfuscation strategies.</param>
    public MainWindowViewModel(
        IDialogService dialogService,
        IFileProcessingOrchestrator orchestrator,
        IPasswordService passwordService,
        IEnumerable<IEncryptionAlgorithmStrategy> encryptionStrategies,
        IEnumerable<IKeyDerivationAlgorithmStrategy> keyDerivationStrategies,
        IEnumerable<INameObfuscationStrategy> nameObfuscationStrategies)
    {
        this.dialogService = dialogService;
        this.orchestrator = orchestrator;
        this.PasswordService = passwordService;

        AvailableEncryptionAlgorithms = new(
            encryptionStrategies
                .OrderBy(a => a.DisplayName));

        AvailableKeyDerivationAlgorithms = new(
            keyDerivationStrategies
                .OrderBy(a => a.DisplayName));

        AvailableNameObfuscationModes = new(
            nameObfuscationStrategies
                .OrderBy(a => a.Id));

        selectedEncryptionAlgorithm = AvailableEncryptionAlgorithms[0].Id;
        selectedKeyDerivationAlgorithm = AvailableKeyDerivationAlgorithms[0].Id;
        selectedNameObfuscationMode = AvailableNameObfuscationModes[0].Id;

        GenerateStrongPasswordCommand = new RelayCommand(GenerateStrongPassword);
        SelectSourceFileCommand = new RelayCommand(SelectSourceFile);
        SelectSourceDirectoryCommand = new RelayCommand(SelectSourceDirectory);
        SelectDestinationPathCommand = new RelayCommand(SelectDestinationPath);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
        ToggleConfirmPasswordVisibilityCommand = new RelayCommand(() => IsConfirmPasswordVisible = !IsConfirmPasswordVisible);

        EncryptFileCommand = new RelayCommand(
            async () =>
            {
                try
                {
                    await ProcessFileAsync(EncryptOperation.Encrypt);
                }
                catch (OperationCanceledException) { }
            },
            CanExecuteProcessFile);

        DecryptFileCommand = new RelayCommand(
            async () =>
            {
                try
                {
                    await ProcessFileAsync(EncryptOperation.Decrypt);
                }
                catch (OperationCanceledException) { }
            },
            CanExecuteProcessFile);

        CancelCommand = new RelayCommand(CancelProcessing, () => IsProcessing);

#if DEBUG
        sourceFilePath = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        destinationPath = @"D:\WorkSpace\EncryptionTest\Encrypted";
#endif
        UpdateControlState();
    }

    /// <summary>
    /// Generates a new strong password using all available character sets, updates the password fields,
    /// optionally copies it to the clipboard, and displays a confirmation dialog.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private void GenerateStrongPassword()
    {
        try
        {
            if (!string.IsNullOrEmpty(Password) &&
                !dialogService.ShowConfirmation(
                    "This will replace your current password. Are you sure you want to generate a new one?",
                    "Replace Password"))
            {
                return;
            }

            int length = 128;
            PasswordGenerationOptions options =
                PasswordGenerationOptions.IncludeUppercase |
                PasswordGenerationOptions.IncludeLowercase |
                PasswordGenerationOptions.IncludeNumbers |
                PasswordGenerationOptions.IncludeSpecialCharacters;

            string generated = PasswordService.GeneratePassword(length, options);
            Password = ConfirmPassword = generated;

            bool copied = TryCopyToClipboard(generated);
            dialogService.ShowMessage(
                BuildPasswordGeneratedMessage(copied),
                "Password Generated",
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex, "generating password");
        }
    }

    /// <summary>
    /// Attempts to place the specified string value onto the system clipboard.
    /// </summary>
    /// <param name="value">The text value to copy. Must not be null.</param>
    /// <returns>true if the clipboard update succeeds; otherwise false.</returns>
    private static bool TryCopyToClipboard(string value)
    {
        try
        {
            System.Windows.Clipboard.SetText(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the multi-line message displayed after a password is generated, including clipboard status.
    /// </summary>
    /// <param name="clipboard">Indicates whether the password was successfully copied to the clipboard.</param>
    /// <returns>A formatted informational message string.</returns>
    private static string BuildPasswordGeneratedMessage(bool clipboard)
    {
        return "🔐 Strong password generated successfully!\n\n" +
               "✅ 128 characters long\n" +
               "✅ Includes uppercase, lowercase, numbers, and symbols\n" +
               (clipboard
                   ? "✅ Copied to clipboard for your convenience\n\n"
                   : "⚠️ Could not copy to clipboard - please copy it manually\n\n") +
               "⚠️ Please store this password securely - it cannot be recovered if lost!";
    }

    /// <summary>
    /// Opens a file selection dialog and assigns the chosen path to <see cref="SourceFilePath"/> if valid.
    /// </summary>
    private void SelectSourceFile()
    {
        SelectPath(dialogService.ShowOpenFileDialog("Select file to encrypt/decrypt"), File.Exists);
    }

    /// <summary>
    /// Opens a folder selection dialog and assigns the chosen directory path to <see cref="SourceFilePath"/> if valid.
    /// </summary>
    private void SelectSourceDirectory()
    {
        SelectPath(dialogService.ShowFolderDialog("Select directory to encrypt/decrypt"), Directory.Exists);
    }

    /// <summary>
    /// Validates and assigns the provided path to <see cref="SourceFilePath"/> if it exists.
    /// Displays a warning dialog if the path is no longer available.
    /// </summary>
    /// <param name="selectedPath">The path selected by the user, or null if cancelled.</param>
    /// <param name="exists">A predicate used to test whether the selected path still exists.</param>
    private void SelectPath(string? selectedPath, Func<string, bool> exists)
    {
        try
        {
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (exists(selectedPath))
                {
                    SourceFilePath = selectedPath;
                }
                else
                {
                    dialogService.ShowMessage(
                        "The selected path no longer exists.",
                        "Not Found",
                        MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(ex, "selecting path");
        }
    }

    /// <summary>
    /// Opens an appropriate dialog for selecting the destination file or folder based on the current source selection.
    /// </summary>
    private void SelectDestinationPath()
    {
        try
        {
            if (File.Exists(SourceFilePath))
            {
                string defaultName = Path.GetFileNameWithoutExtension(SourceFilePath);
                string? selectedPath = dialogService.ShowSaveFileDialog(
                    "Select destination for processed file",
                    "All files (*.*)|*.*",
                    defaultName);

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DestinationPath = selectedPath;
                }
            }
            else
            {
                string? selectedPath = dialogService.ShowFolderDialog("Select destination directory");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DestinationPath = selectedPath;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(ex, "selecting destination");
        }
    }

    /// <summary>
    /// Determines whether the encryption/decryption command can currently execute based on required input fields and processing state.
    /// </summary>
    /// <returns>true if all required values are provided and no operation is running; otherwise false.</returns>
    private bool CanExecuteProcessFile()
    {
        return !IsProcessing &&
               new[] { SourceFilePath, DestinationPath, Password, ConfirmPassword }
                   .All(s => !string.IsNullOrWhiteSpace(s));
    }

    /// <summary>
    /// Executes the selected encryption or decryption operation, performing validation, warning analysis,
    /// progress tracking, and error reporting. Displays results or messages to the user via dialogs.
    /// </summary>
    /// <param name="operation">The cryptographic operation to perform (encrypt or decrypt).</param>
    /// <returns>A task that completes when processing finishes or is cancelled.</returns>
    /// <exception cref="OperationCanceledException">May be internally thrown and caught if the operation is cancelled.</exception>
    private async Task ProcessFileAsync(EncryptOperation operation)
    {
        FileProcessingOrchestratorRequest request = new(
            SourceFilePath,
            DestinationPath,
            Password,
            ConfirmPassword,
            SelectedEncryptionAlgorithm,
            SelectedKeyDerivationAlgorithm,
            operation,
            SelectedNameObfuscationMode);

        IReadOnlyList<string> validationErrors = await orchestrator.ValidateAsync(request);
        if (validationErrors.Any())
        {
            dialogService.ShowValidationErrors(validationErrors);
            return;
        }

        IReadOnlyList<string> warnings = await orchestrator.AnalyzeWarningsAsync(request);
        if (warnings.Any())
        {
            string warningMessage = $"⚠️ Please review the following concerns:\n\n{string.Join("\n\n", warnings.Select(w => $"• {w}"))}";
            if (!dialogService.ShowConfirmation(
                    $"{warningMessage}\n\nDo you want to continue with the {operation.ToString().ToLower()} operation?",
                    "Confirm Operation"))
            {
                throw new OperationCanceledException("Operation cancelled by user due to warnings.");
            }
        }

        IsProcessing = true;
        string operationText = operation == EncryptOperation.Encrypt ? "Encrypting" : "Decrypting";

        try
        {
            Progress<FileProcessingStatus> progress = new(OnProgressUpdate);
            cancellationTokenSource = new();
            Result<FileProcessingResult> result = await orchestrator.ExecuteAsync(
                request,
                progress,
                cancellationTokenSource.Token);

            if (result.IsSuccess && result.Value != null)
            {
                string sourceType = File.Exists(SourceFilePath) ? "file" : "directory";
                dialogService.ShowProcessingResult(result.Value, operation, sourceType);
            }
            else
            {
                dialogService.ShowMessage(
                    $"Failed to {operation.ToString().ToLower()}: {string.Join(", ", result.Errors)}",
                    "Error",
                    MessageBoxImage.Error);
            }
        }
        catch (OperationCanceledException)
        {
            dialogService.ShowMessage($"{operationText} was cancelled by user.", "Operation Cancelled", MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex, operationText.ToLower(), operation);
        }
        finally
        {
            IsProcessing = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Requests cancellation of the current processing operation.
    /// </summary>
    private void CancelProcessing()
    {
        try
        {
            cancellationTokenSource?.Cancel();
            ProgressText = "Cancelling...";
        }
        catch (Exception ex)
        {
            ShowError(ex, "cancelling operation");
        }
        finally
        {
            ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Updates UI-bound progress values and estimated completion time based on the latest processing status.
    /// </summary>
    /// <param name="update">The current file processing status snapshot.</param>
    private void OnProgressUpdate(FileProcessingStatus update)
    {
        double progress = update.TotalBytes > 0 ? (double)update.ProcessedBytes / update.TotalBytes * 100 : 100;
        double bytesPerSecond = update.Elapsed.TotalSeconds > 0 ? update.ProcessedBytes / update.Elapsed.TotalSeconds : 0;
        TimeSpan eta = update.TotalBytes > 0 && bytesPerSecond > 0
            ? TimeSpan.FromSeconds((update.TotalBytes - update.ProcessedBytes) / bytesPerSecond)
            : TimeSpan.Zero;

        ProgressValue = progress;
        ProgressText = $"Processing: {update.ProcessedFiles}/{update.TotalFiles} files ({progress:F1}%) - ETA: {eta:hh\\:mm\\:ss}";
    }

    /// <summary>
    /// Adjusts control availability and resets progress information when processing state changes.
    /// </summary>
    private void UpdateControlState()
    {
        AreControlsEnabled = !IsProcessing;
        if (!IsProcessing)
        {
            ProgressValue = 0;
            ProgressText = string.Empty;
        }
    }

    /// <summary>
    /// Requests re-evaluation of the CanExecute state for encryption and decryption commands.
    /// </summary>
    private void RefreshProcessCommands()
    {
        ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
        ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
        ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Displays an error dialog constructed from an exception and processing context information.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="context">A short description of the operation context where the error occurred.</param>
    /// <param name="operation">Optional cryptographic operation being performed when the error occurred.</param>
    private void ShowError(Exception ex, string context, EncryptOperation? operation = null)
    {
        (string title, string message) = BuildErrorMessage(ex, context, operation);
        dialogService.ShowMessage(message, title, MessageBoxImage.Error);
    }

    /// <summary>
    /// Builds a user-friendly error title and message (including contextual advice when possible) from an exception.
    /// </summary>
    /// <param name="ex">The exception to interpret.</param>
    /// <param name="context">A short descriptor of the action attempted.</param>
    /// <param name="operation">An optional cryptographic operation for additional context.</param>
    /// <returns>A tuple containing the dialog title and composed message text.</returns>
    private static (string Title, string Message) BuildErrorMessage(Exception ex, string context, EncryptOperation? operation)
    {
        string raw = ex.Message ?? "Unknown error.";
        string lower = raw.ToLowerInvariant();
        StringComparison casePolicy = StringComparison.InvariantCultureIgnoreCase;

        (string Title, string Advice)? rule = lower switch
        {
            var s when s.Contains("access denied", casePolicy) =>
                ("Access Denied", "Check file or folder permissions or run as administrator."),
            var s when s.Contains("insufficient disk space", casePolicy) =>
                ("Insufficient Disk Space", "Free disk space or choose another destination."),
            var s when s.Contains("invalid password", casePolicy) =>
                ("Invalid Password", "Verify the password and try again."),
            var s when s.Contains("corrupted", casePolicy) =>
                ("File Corruption", "The file may be damaged or not properly encrypted."),
            var s when s.Contains("key derivation", casePolicy) =>
                ("Key Derivation Error", "A problem occurred while deriving the encryption key."),
            _ => null,
        };

        if (rule is { } r)
        {
            return (r.Title, $"{raw}\n\n{r.Advice}");
        }

        string opText = operation is null ? string.Empty : $" during {operation.Value.ToString().ToLower()}";
        return ("Operation Failed", $"An error occurred{opText} while {context}: {raw}");
    }
}
