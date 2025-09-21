using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Presentation.Commands;
using CloudZCrypt.WPF.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CloudZCrypt.WPF.ViewModels;

public class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService dialogService;
    private readonly IFileProcessingOrchestrator orchestrator;
    private readonly IPasswordService passwordService;

    private CancellationTokenSource? cancellationTokenSource;

    private static readonly Dictionary<PasswordStrength, SolidColorBrush> strengthColorCache = new()
    {
        [PasswordStrength.VeryWeak] = new(System.Windows.Media.Color.FromRgb(220, 53, 69)),
        [PasswordStrength.Weak] = new(System.Windows.Media.Color.FromRgb(255, 193, 7)),
        [PasswordStrength.Fair] = new(System.Windows.Media.Color.FromRgb(255, 193, 7)),
        [PasswordStrength.Good] = new(System.Windows.Media.Color.FromRgb(40, 167, 69)),
        [PasswordStrength.Strong] = new(System.Windows.Media.Color.FromRgb(25, 135, 84)),
    };

    private string sourceFilePath = string.Empty;
    private string destinationPath = string.Empty;
    private string password = string.Empty;
    private string confirmPassword = string.Empty;
    private bool isPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;
    private EncryptionAlgorithm selectedEncryptionAlgorithm;
    private KeyDerivationAlgorithm selectedKeyDerivationAlgorithm;
    private bool isProcessing;
    private double progressValue;
    private string progressText = string.Empty;
    private bool areControlsEnabled = true;
    private double passwordStrengthScore;
    private string passwordStrengthText = string.Empty;
    private System.Windows.Media.Brush passwordStrengthColor = System
        .Windows
        .Media
        .Brushes
        .Transparent;
    private Visibility passwordStrengthVisibility = Visibility.Hidden;
    private double confirmPasswordStrengthScore;
    private string confirmPasswordStrengthText = string.Empty;
    private System.Windows.Media.Brush confirmPasswordStrengthColor = System
        .Windows
        .Media
        .Brushes
        .Transparent;
    private Visibility confirmPasswordStrengthVisibility = Visibility.Hidden;

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
    public string Password
    {
        get => password;
        set
        {
            if (SetProperty(ref password, value))
            {
                _ = UpdatePasswordStrengthAsync(value, false);
                RefreshProcessCommands();
            }
        }
    }
    public string ConfirmPassword
    {
        get => confirmPassword;
        set
        {
            if (SetProperty(ref confirmPassword, value))
            {
                _ = UpdatePasswordStrengthAsync(value, true);
                RefreshProcessCommands();
            }
        }
    }
    public bool IsPasswordVisible
    {
        get => isPasswordVisible;
        set => SetProperty(ref isPasswordVisible, value);
    }
    public bool IsConfirmPasswordVisible
    {
        get => isConfirmPasswordVisible;
        set => SetProperty(ref isConfirmPasswordVisible, value);
    }
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
    public EncryptionAlgorithmViewModel? SelectedEncryptionAlgorithmInfo =>
        AvailableEncryptionAlgorithms.FirstOrDefault(a => a.Id == selectedEncryptionAlgorithm);
    public KeyDerivationAlgorithmViewModel? SelectedKeyDerivationAlgorithmInfo =>
        AvailableKeyDerivationAlgorithms.FirstOrDefault(a =>
            a.Id == selectedKeyDerivationAlgorithm
        );
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
    public double ProgressValue
    {
        get => progressValue;
        set => SetProperty(ref progressValue, value);
    }
    public string ProgressText
    {
        get => progressText;
        set => SetProperty(ref progressText, value);
    }
    public bool AreControlsEnabled
    {
        get => areControlsEnabled;
        set => SetProperty(ref areControlsEnabled, value);
    }
    public double PasswordStrengthScore
    {
        get => passwordStrengthScore;
        set => SetProperty(ref passwordStrengthScore, value);
    }
    public string PasswordStrengthText
    {
        get => passwordStrengthText;
        set => SetProperty(ref passwordStrengthText, value);
    }
    public System.Windows.Media.Brush PasswordStrengthColor
    {
        get => passwordStrengthColor;
        set => SetProperty(ref passwordStrengthColor, value);
    }
    public Visibility PasswordStrengthVisibility
    {
        get => passwordStrengthVisibility;
        set => SetProperty(ref passwordStrengthVisibility, value);
    }
    public double ConfirmPasswordStrengthScore
    {
        get => confirmPasswordStrengthScore;
        set => SetProperty(ref confirmPasswordStrengthScore, value);
    }
    public string ConfirmPasswordStrengthText
    {
        get => confirmPasswordStrengthText;
        set => SetProperty(ref confirmPasswordStrengthText, value);
    }
    public System.Windows.Media.Brush ConfirmPasswordStrengthColor
    {
        get => confirmPasswordStrengthColor;
        set => SetProperty(ref confirmPasswordStrengthColor, value);
    }
    public Visibility ConfirmPasswordStrengthVisibility
    {
        get => confirmPasswordStrengthVisibility;
        set => SetProperty(ref confirmPasswordStrengthVisibility, value);
    }

    public ObservableCollection<EncryptionAlgorithmViewModel> AvailableEncryptionAlgorithms { get; }
    public ObservableCollection<KeyDerivationAlgorithmViewModel> AvailableKeyDerivationAlgorithms { get; }

    public ICommand GenerateStrongPasswordCommand { get; }
    public ICommand SelectSourceFileCommand { get; }
    public ICommand SelectSourceDirectoryCommand { get; }
    public ICommand SelectDestinationPathCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand ToggleConfirmPasswordVisibilityCommand { get; }
    public ICommand EncryptFileCommand { get; }
    public ICommand DecryptFileCommand { get; }

    public MainWindowViewModel(
        IDialogService dialogService,
        IFileProcessingOrchestrator orchestrator,
        IPasswordService passwordService,
        IEnumerable<IEncryptionAlgorithmStrategy> encryptionStrategies,
        IEnumerable<IKeyDerivationAlgorithmStrategy> keyDerivationStrategies
    )
    {
        this.dialogService = dialogService;
        this.orchestrator = orchestrator;
        this.passwordService = passwordService;

        AvailableEncryptionAlgorithms = new(
            encryptionStrategies
                .Select(EncryptionAlgorithmViewModel.FromStrategy)
                .OrderBy(a => a.DisplayName)
        );
        AvailableKeyDerivationAlgorithms = new(
            keyDerivationStrategies
                .Select(KeyDerivationAlgorithmViewModel.FromStrategy)
                .OrderBy(a => a.DisplayName)
        );
        selectedEncryptionAlgorithm = AvailableEncryptionAlgorithms.First().Id;
        selectedKeyDerivationAlgorithm = AvailableKeyDerivationAlgorithms.First().Id;

        GenerateStrongPasswordCommand = new RelayCommand(GenerateStrongPassword);
        SelectSourceFileCommand = new RelayCommand(SelectSourceFile);
        SelectSourceDirectoryCommand = new RelayCommand(SelectSourceDirectory);
        SelectDestinationPathCommand = new RelayCommand(SelectDestinationPath);
        TogglePasswordVisibilityCommand = new RelayCommand(() =>
            IsPasswordVisible = !IsPasswordVisible
        );
        ToggleConfirmPasswordVisibilityCommand = new RelayCommand(() =>
            IsConfirmPasswordVisible = !IsConfirmPasswordVisible
        );
        EncryptFileCommand = new RelayCommand(
            async () =>
            {
                try
                {
                    await ProcessFileAsync(EncryptOperation.Encrypt);
                }
                catch (OperationCanceledException) { }
            },
            CanExecuteProcessFile
        );
        DecryptFileCommand = new RelayCommand(
            async () =>
            {
                try
                {
                    await ProcessFileAsync(EncryptOperation.Decrypt);
                }
                catch (OperationCanceledException) { }
            },
            CanExecuteProcessFile
        );
#if DEBUG
        sourceFilePath = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        destinationPath = @"D:\WorkSpace\EncryptionTest\Encrypted";
#endif
        UpdateControlState();
    }

    private async Task GenerateStrongPassword()
    {
        try
        {
            if (
                !string.IsNullOrEmpty(Password)
                && !dialogService.ShowConfirmation(
                    "This will replace your current password. Are you sure you want to generate a new one?",
                    "Replace Password"
                )
            )
            {
                return;
            }
            // replicate previous defaults
            int length = 128;
            PasswordGenerationOptions options =
                PasswordGenerationOptions.IncludeUppercase
                | PasswordGenerationOptions.IncludeLowercase
                | PasswordGenerationOptions.IncludeNumbers
                | PasswordGenerationOptions.IncludeSpecialCharacters;
            string generated = passwordService.GeneratePassword(length, options);
            Password = ConfirmPassword = generated;
            bool copied = TryCopyToClipboard(generated);
            dialogService.ShowMessage(
                BuildPasswordGeneratedMessage(copied),
                "Password Generated",
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            ShowError(ex, "generating password");
        }
    }

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

    private static string BuildPasswordGeneratedMessage(bool clipboard)
    {
        return "🔐 Strong password generated successfully!\n\n"
            + "✅ 128 characters long\n"
            + "✅ Includes uppercase, lowercase, numbers, and symbols\n"
            + (
                clipboard
                    ? "✅ Copied to clipboard for your convenience\n\n"
                    : "⚠️ Could not copy to clipboard - please copy it manually\n\n"
            )
            + "⚠️ Please store this password securely - it cannot be recovered if lost!";
    }

    private void SelectSourceFile()
    {
        SelectPath(dialogService.ShowOpenFileDialog("Select file to encrypt/decrypt"), File.Exists);
    }

    private void SelectSourceDirectory()
    {
        SelectPath(
            dialogService.ShowFolderDialog("Select directory to encrypt/decrypt"),
            Directory.Exists
        );
    }

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
                        MessageBoxImage.Warning
                    );
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(ex, "selecting path");
        }
    }

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
                    defaultName
                );
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DestinationPath = selectedPath;
                }
            }
            else
            {
                string? selectedPath = dialogService.ShowFolderDialog(
                    "Select destination directory"
                );
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

    private bool CanExecuteProcessFile()
    {
        return !IsProcessing
            && new[] { SourceFilePath, DestinationPath, Password, ConfirmPassword }.All(s =>
                !string.IsNullOrWhiteSpace(s)
            );
    }

    private async Task ProcessFileAsync(EncryptOperation operation)
    {
        FileProcessingOrchestratorRequest request = new(
            SourceFilePath,
            DestinationPath,
            Password,
            ConfirmPassword,
            SelectedEncryptionAlgorithm,
            SelectedKeyDerivationAlgorithm,
            operation
        );
        IReadOnlyList<string> validationErrors = await orchestrator.ValidateAsync(request);
        if (validationErrors.Any())
        {
            dialogService.ShowValidationErrors(validationErrors);
            return;
        }
        IReadOnlyList<string> warnings = await orchestrator.AnalyzeWarningsAsync(request);
        if (warnings.Any())
        {
            string warningMessage =
                $"⚠️ Please review the following concerns:\n\n{string.Join("\n\n", warnings.Select(w => $"• {w}"))}";
            if (
                !dialogService.ShowConfirmation(
                    $"{warningMessage}\n\nDo you want to continue with the {operation.ToString().ToLower()} operation?",
                    "Confirm Operation"
                )
            )
            {
                throw new OperationCanceledException(
                    "Operation cancelled by user due to warnings."
                );
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
                cancellationTokenSource.Token
            );
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
                    MessageBoxImage.Error
                );
            }
        }
        catch (OperationCanceledException)
        {
            dialogService.ShowMessage(
                $"{operationText} was cancelled by user.",
                "Operation Cancelled",
                MessageBoxImage.Information
            );
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

    private void OnProgressUpdate(FileProcessingStatus update)
    {
        double progress =
            update.TotalBytes > 0 ? (double)update.ProcessedBytes / update.TotalBytes * 100 : 100;
        double bytesPerSecond = update.ProcessedBytes / update.Elapsed.TotalSeconds;
        TimeSpan eta =
            update.TotalBytes > 0 && bytesPerSecond > 0
                ? TimeSpan.FromSeconds((update.TotalBytes - update.ProcessedBytes) / bytesPerSecond)
                : TimeSpan.Zero;
        ProgressValue = progress;
        ProgressText =
            $"Processing: {update.ProcessedFiles}/{update.TotalFiles} files ({progress:F1}%) - ETA: {eta:hh\\:mm\\:ss}";
    }

    private void UpdateControlState()
    {
        AreControlsEnabled = !IsProcessing;
        if (!IsProcessing)
        {
            ProgressValue = 0;
            ProgressText = string.Empty;
        }
    }

    private async Task UpdatePasswordStrengthAsync(string pwd, bool isConfirmField)
    {
        if (string.IsNullOrEmpty(pwd))
        {
            HideStrength(isConfirmField);
            return;
        }
        Domain.ValueObjects.Password.PasswordStrengthAnalysis analysis =
            passwordService.AnalyzePasswordStrength(pwd);
        ApplyStrengthResult(
            isConfirmField,
            new Application.DataTransferObjects.Passwords.PasswordStrengthResult(
                analysis.Strength,
                analysis.Description,
                analysis.Score
            )
        );
    }

    private void ApplyStrengthResult(
        bool isConfirmField,
        Application.DataTransferObjects.Passwords.PasswordStrengthResult strengthResult
    )
    {
        System.Windows.Media.Brush color = GetStrengthColor(strengthResult.Strength);
        if (isConfirmField)
        {
            ConfirmPasswordStrengthScore = strengthResult.Score;
            ConfirmPasswordStrengthText = strengthResult.Description;
            ConfirmPasswordStrengthColor = color;
            ConfirmPasswordStrengthVisibility = Visibility.Visible;
        }
        else
        {
            PasswordStrengthScore = strengthResult.Score;
            PasswordStrengthText = strengthResult.Description;
            PasswordStrengthColor = color;
            PasswordStrengthVisibility = Visibility.Visible;
        }
    }

    private void HideStrength(bool isConfirmField)
    {
        _ = isConfirmField
            ? ConfirmPasswordStrengthVisibility = Visibility.Hidden
            : PasswordStrengthVisibility = Visibility.Hidden;
    }

    private static System.Windows.Media.Brush GetStrengthColor(PasswordStrength strength)
    {
        return strengthColorCache.GetValueOrDefault(
            strength,
            System.Windows.Media.Brushes.Transparent
        );
    }

    private void RefreshProcessCommands()
    {
        ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
        ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
    }

    private void ShowError(Exception ex, string context, EncryptOperation? operation = null)
    {
        (string title, string message) = BuildErrorMessage(ex, context, operation);
        dialogService.ShowMessage(message, title, MessageBoxImage.Error);
    }

    private static (string Title, string Message) BuildErrorMessage(
        Exception ex,
        string context,
        EncryptOperation? operation
    )
    {
        string raw = ex.Message ?? "Unknown error.";
        string lower = raw.ToLowerInvariant();
        StringComparison casePolicy = StringComparison.InvariantCultureIgnoreCase;
        (string Title, string Advice)? rule = lower switch
        {
            var s when s.Contains("access denied", casePolicy) => (
                "Access Denied",
                "Check file or folder permissions or run as administrator."
            ),
            var s when s.Contains("insufficient disk space", casePolicy) => (
                "Insufficient Disk Space",
                "Free disk space or choose another destination."
            ),
            var s when s.Contains("invalid password", casePolicy) => (
                "Invalid Password",
                "Verify the password and try again."
            ),
            var s when s.Contains("corrupted", casePolicy) => (
                "File Corruption",
                "The file may be damaged or not properly encrypted."
            ),
            var s when s.Contains("key derivation", casePolicy) => (
                "Key Derivation Error",
                "A problem occurred while deriving the encryption key."
            ),
            _ => null,
        };
        if (rule is { } r)
        {
            return (r.Title, $"{raw}\n\n{r.Advice}");
        }
        string opText = operation is null
            ? string.Empty
            : $" during {operation.Value.ToString().ToLower()}";
        return ("Operation Failed", $"An error occurred{opText} while {context}: {raw}");
    }

    public void Dispose()
    {
        cancellationTokenSource?.Dispose();
    }
}
