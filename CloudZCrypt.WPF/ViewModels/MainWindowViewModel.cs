using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Queries;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Presentation.Commands;
using CloudZCrypt.WPF.Services.Interfaces;
using MediatR;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CloudZCrypt.WPF.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IFileProcessingOrchestrator _orchestrator;

    private CancellationTokenSource? _cancellationTokenSource;

    // Cached brushes to avoid recreation
    private static readonly Dictionary<PasswordStrength, SolidColorBrush> _strengthColorCache = new()
    {
        [PasswordStrength.VeryWeak] = new(System.Windows.Media.Color.FromRgb(220, 53, 69)),
        [PasswordStrength.Weak] = new(System.Windows.Media.Color.FromRgb(255, 193, 7)),
        [PasswordStrength.Fair] = new(System.Windows.Media.Color.FromRgb(255, 193, 7)),
        [PasswordStrength.Good] = new(System.Windows.Media.Color.FromRgb(40, 167, 69)),
        [PasswordStrength.Strong] = new(System.Windows.Media.Color.FromRgb(25, 135, 84))
    };

    // Private fields for properties
    private string _sourceFilePath = string.Empty;
    private string _destinationPath = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isPasswordVisible = false;
    private bool _isConfirmPasswordVisible = false;
    private EncryptionAlgorithm _selectedEncryptionAlgorithm;
    private KeyDerivationAlgorithm _selectedKeyDerivationAlgorithm; // keep enum for processing
    private bool _isProcessing;
    private double _progressValue;
    private string _progressText = string.Empty;
    private bool _areControlsEnabled = true;
    private double _passwordStrengthScore;
    private string _passwordStrengthText = string.Empty;
    private System.Windows.Media.Brush _passwordStrengthColor = System.Windows.Media.Brushes.Transparent;
    private Visibility _passwordStrengthVisibility = Visibility.Hidden;
    private double _confirmPasswordStrengthScore;
    private string _confirmPasswordStrengthText = string.Empty;
    private System.Windows.Media.Brush _confirmPasswordStrengthColor = System.Windows.Media.Brushes.Transparent;
    private Visibility _confirmPasswordStrengthVisibility = Visibility.Hidden;

    #endregion

    #region Public Properties

    public string SourceFilePath
    {
        get => _sourceFilePath;
        set
        {
            if (SetProperty(ref _sourceFilePath, value))
            {
                ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value))
            {
                ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                _ = UpdatePasswordStrengthAsync(value, isConfirmField: false);
                ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                _ = UpdatePasswordStrengthAsync(value, isConfirmField: true);
                ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    public bool IsConfirmPasswordVisible
    {
        get => _isConfirmPasswordVisible;
        set => SetProperty(ref _isConfirmPasswordVisible, value);
    }

    public EncryptionAlgorithm SelectedEncryptionAlgorithm
    {
        get => _selectedEncryptionAlgorithm;
        set
        {
            if (SetProperty(ref _selectedEncryptionAlgorithm, value))
            {
                OnPropertyChanged(nameof(SelectedEncryptionAlgorithmInfo));
            }
        }
    }

    public KeyDerivationAlgorithm SelectedKeyDerivationAlgorithm
    {
        get => _selectedKeyDerivationAlgorithm;
        set
        {
            if (SetProperty(ref _selectedKeyDerivationAlgorithm, value))
            {
                OnPropertyChanged(nameof(SelectedKeyDerivationAlgorithmInfo));
            }
        }
    }

    public EncryptionAlgorithmViewModel? SelectedEncryptionAlgorithmInfo =>
        AvailableEncryptionAlgorithms.FirstOrDefault(a => a.Id == _selectedEncryptionAlgorithm);

    public KeyDerivationAlgorithmViewModel? SelectedKeyDerivationAlgorithmInfo =>
        AvailableKeyDerivationAlgorithms.FirstOrDefault(a => a.Id == _selectedKeyDerivationAlgorithm);

    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                UpdateControlState();
                ((RelayCommand)EncryptFileCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DecryptFileCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public bool AreControlsEnabled
    {
        get => _areControlsEnabled;
        set => SetProperty(ref _areControlsEnabled, value);
    }

    public double PasswordStrengthScore
    {
        get => _passwordStrengthScore;
        set => SetProperty(ref _passwordStrengthScore, value);
    }

    public string PasswordStrengthText
    {
        get => _passwordStrengthText;
        set => SetProperty(ref _passwordStrengthText, value);
    }

    public System.Windows.Media.Brush PasswordStrengthColor
    {
        get => _passwordStrengthColor;
        set => SetProperty(ref _passwordStrengthColor, value);
    }

    public Visibility PasswordStrengthVisibility
    {
        get => _passwordStrengthVisibility;
        set => SetProperty(ref _passwordStrengthVisibility, value);
    }

    public double ConfirmPasswordStrengthScore
    {
        get => _confirmPasswordStrengthScore;
        set => SetProperty(ref _confirmPasswordStrengthScore, value);
    }

    public string ConfirmPasswordStrengthText
    {
        get => _confirmPasswordStrengthText;
        set => SetProperty(ref _confirmPasswordStrengthText, value);
    }

    public System.Windows.Media.Brush ConfirmPasswordStrengthColor
    {
        get => _confirmPasswordStrengthColor;
        set => SetProperty(ref _confirmPasswordStrengthColor, value);
    }

    public Visibility ConfirmPasswordStrengthVisibility
    {
        get => _confirmPasswordStrengthVisibility;
        set => SetProperty(ref _confirmPasswordStrengthVisibility, value);
    }

    #endregion

    #region Collections

    // Replace enum collection with strategy descriptors
    public ObservableCollection<EncryptionAlgorithmViewModel> AvailableEncryptionAlgorithms { get; }
    public ObservableCollection<KeyDerivationAlgorithmViewModel> AvailableKeyDerivationAlgorithms { get; } // cambiado a ViewModel

    #endregion

    #region Commands

    public ICommand GenerateStrongPasswordCommand { get; }
    public ICommand SelectSourceFileCommand { get; }
    public ICommand SelectSourceDirectoryCommand { get; }
    public ICommand SelectDestinationPathCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand ToggleConfirmPasswordVisibilityCommand { get; }
    public ICommand EncryptFileCommand { get; }
    public ICommand DecryptFileCommand { get; }

    #endregion

    #region Constructor

    public MainWindowViewModel(
        IDialogService dialogService,
        IMediator mediator,
        IFileProcessingOrchestrator orchestrator,
        IEnumerable<IEncryptionAlgorithmStrategy> encryptionStrategies,
        IEnumerable<IKeyDerivationAlgorithmStrategy> keyDerivationStrategies)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _orchestrator = orchestrator;

        AvailableEncryptionAlgorithms = new ObservableCollection<EncryptionAlgorithmViewModel>(
            encryptionStrategies.Select(EncryptionAlgorithmViewModel.FromStrategy)
                                 .OrderBy(a => a.DisplayName));

        AvailableKeyDerivationAlgorithms = new ObservableCollection<KeyDerivationAlgorithmViewModel>(
            keyDerivationStrategies.Select(KeyDerivationAlgorithmViewModel.FromStrategy)
                                   .OrderBy(a => a.DisplayName));

        _selectedEncryptionAlgorithm = AvailableEncryptionAlgorithms.First().Id;
        _selectedKeyDerivationAlgorithm = AvailableKeyDerivationAlgorithms.First().Id;

        // Initialize commands
        GenerateStrongPasswordCommand = new RelayCommand(GenerateStrongPassword);
        SelectSourceFileCommand = new RelayCommand(SelectSourceFile);
        SelectSourceDirectoryCommand = new RelayCommand(SelectSourceDirectory);
        SelectDestinationPathCommand = new RelayCommand(SelectDestinationPath);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
        ToggleConfirmPasswordVisibilityCommand = new RelayCommand(() => IsConfirmPasswordVisible = !IsConfirmPasswordVisible);

        EncryptFileCommand = new RelayCommand(async () =>
        {
            try { await ProcessFileAsync(EncryptOperation.Encrypt); }
            catch (OperationCanceledException) { /* Ignore */ }
        }, CanExecuteProcessFile);

        DecryptFileCommand = new RelayCommand(async () =>
        {
            try { await ProcessFileAsync(EncryptOperation.Decrypt); }
            catch (OperationCanceledException) { /* Ignore */ }
        }, CanExecuteProcessFile);

#if DEBUG
        _sourceFilePath = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        _destinationPath = @"D:\WorkSpace\EncryptionTest\Encrypted";
#endif
        UpdateControlState();
    }

    #endregion

    #region Command Methods

    private async Task GenerateStrongPassword()
    {
        try
        {
            // Show confirmation for replacing existing password
            if (!string.IsNullOrEmpty(Password))
            {
                bool shouldReplace = _dialogService.ShowConfirmation(
                    "This will replace your current password. Are you sure you want to generate a new one?",
                    "Replace Password");

                if (!shouldReplace)
                    return;
            }

            // Updated to primary-constructor record
            GeneratePasswordQuery query = new(
                Length: 128,
                IncludeUppercase: true,
                IncludeLowercase: true,
                IncludeNumbers: true,
                IncludeSpecialCharacters: true,
                ExcludeSimilarCharacters: false);

            Result<string> result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                Password = result.Value;
                ConfirmPassword = result.Value;

                try
                {
                    System.Windows.Clipboard.SetText(result.Value);
                    _dialogService.ShowMessage(
                        "🔐 Strong password generated successfully!\n\n" +
                        "✅ 128 characters long\n" +
                        "✅ Includes uppercase, lowercase, numbers, and symbols\n" +
                        "✅ Copied to clipboard for your convenience\n\n" +
                        "⚠️ Please store this password securely - it cannot be recovered if lost!",
                        "Password Generated",
                        MessageBoxImage.Information);
                }
                catch
                {
                    _dialogService.ShowMessage(
                        "🔐 Strong password generated successfully!\n\n" +
                        "✅ 128 characters long\n" +
                        "✅ Includes uppercase, lowercase, numbers, and symbols\n\n" +
                        "⚠️ Could not copy to clipboard - please copy it manually\n" +
                        "⚠️ Please store this password securely - it cannot be recovered if lost!",
                        "Password Generated",
                        MessageBoxImage.Information);
                }
            }
            else
            {
                _dialogService.ShowMessage($"Failed to generate password:\n\n{string.Join("\n", result.Errors.Select(e => $"• {e}"))}", "Password Generation Error", MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Unexpected error while generating password: {ex.Message}", "Password Generation Error", MessageBoxImage.Error);
        }
    }

    private void SelectSourceFile()
    {
        try
        {
            string? selectedPath = _dialogService.ShowOpenFileDialog("Select file to encrypt/decrypt");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (File.Exists(selectedPath))
                {
                    SourceFilePath = selectedPath;
                }
                else
                {
                    _dialogService.ShowMessage("The selected file no longer exists.", "File Not Found", MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Error selecting file: {ex.Message}", "File Selection Error", MessageBoxImage.Error);
        }
    }

    private void SelectSourceDirectory()
    {
        try
        {
            string? selectedPath = _dialogService.ShowFolderDialog("Select directory to encrypt/decrypt");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (Directory.Exists(selectedPath))
                {
                    SourceFilePath = selectedPath;
                }
                else
                {
                    _dialogService.ShowMessage("The selected directory no longer exists.", "Directory Not Found", MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Error selecting directory: {ex.Message}", "Directory Selection Error", MessageBoxImage.Error);
        }
    }

    private void SelectDestinationPath()
    {
        try
        {
            if (File.Exists(SourceFilePath))
            {
                // For single file, show save dialog
                string defaultName = Path.GetFileNameWithoutExtension(SourceFilePath);
                string? selectedPath = _dialogService.ShowSaveFileDialog("Select destination for processed file", "All files (*.*)|*.*", defaultName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DestinationPath = selectedPath;
                }
            }
            else
            {
                // For directory, show folder dialog
                string? selectedPath = _dialogService.ShowFolderDialog("Select destination directory");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    DestinationPath = selectedPath;
                }
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Error selecting destination: {ex.Message}", "Destination Selection Error", MessageBoxImage.Error);
        }
    }

    private bool CanExecuteProcessFile()
    {
        return !IsProcessing &&
               !string.IsNullOrWhiteSpace(SourceFilePath) &&
               !string.IsNullOrWhiteSpace(DestinationPath) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private async Task ProcessFileAsync(EncryptOperation operation)
    {
        // Build request DTO for orchestration
        FileProcessingOrchestratorRequest request = new(
            SourceFilePath,
            DestinationPath,
            Password,
            ConfirmPassword,
            SelectedEncryptionAlgorithm,
            SelectedKeyDerivationAlgorithm,
            operation);

        // Validate inputs using orchestrator
        IReadOnlyList<string> validationErrors = await _orchestrator.ValidateAsync(request);

        if (validationErrors.Any())
        {
            _dialogService.ShowValidationErrors(validationErrors);
            return;
        }

        // Analyze warnings
        IReadOnlyList<string> warnings = await _orchestrator.AnalyzeWarningsAsync(request);

        if (warnings.Any())
        {
            string warningMessage = $"⚠️ Please review the following concerns:\n\n{string.Join("\n\n", warnings.Select(w => $"• {w}"))}";
            bool shouldContinue = _dialogService.ShowConfirmation(
                $"{warningMessage}\n\nDo you want to continue with the {operation.ToString().ToLower()} operation?",
                "Confirm Operation");

            if (!shouldContinue)
            {
                throw new OperationCanceledException("Operation cancelled by user due to warnings.");
            }
        }

        IsProcessing = true;
        string operationText = operation == EncryptOperation.Encrypt ? "Encrypting" : "Decrypting";

        try
        {
            Progress<FileProcessingStatus> progress = new(OnProgressUpdate);
            _cancellationTokenSource = new CancellationTokenSource();

            Result<FileProcessingResult> result = await _orchestrator.ExecuteAsync(
                request,
                progress,
                _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Value != null)
            {
                string sourceType = File.Exists(SourceFilePath) ? "file" : "directory";
                _dialogService.ShowProcessingResult(result.Value, operation, sourceType);
            }
            else
            {
                _dialogService.ShowMessage($"Failed to {operation.ToString().ToLower()}: {string.Join(", ", result.Errors)}", "Error", MessageBoxImage.Error);
            }
        }
        catch (OperationCanceledException)
        {
            _dialogService.ShowMessage($"{operationText} was cancelled by user.", "Operation Cancelled", MessageBoxImage.Information);
        }
        catch (UnauthorizedAccessException ex)
        {
            _dialogService.ShowMessage($"Access denied: {ex.Message}\n\nPlease check file permissions and try running as administrator if necessary.", "Access Denied", MessageBoxImage.Error);
        }
        catch (DirectoryNotFoundException ex)
        {
            _dialogService.ShowMessage($"Directory not found: {ex.Message}\n\nPlease verify the path exists and is accessible.", "Directory Not Found", MessageBoxImage.Error);
        }
        catch (FileNotFoundException ex)
        {
            _dialogService.ShowMessage($"File not found: {ex.Message}\n\nPlease verify the file exists and is accessible.", "File Not Found", MessageBoxImage.Error);
        }
        catch (IOException ex)
        {
            _dialogService.ShowMessage($"I/O error occurred: {ex.Message}\n\nThis might be due to insufficient disk space, file locks, or hardware issues.", "I/O Error", MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // Handle specific error messages from the enhanced error handling
            string errorMessage = ex.Message;
            string errorTitle = "Error";

            if (errorMessage.Contains("access denied", StringComparison.OrdinalIgnoreCase))
            {
                errorTitle = "Access Denied";
                errorMessage += "\n\nPlease check file permissions and try running as administrator if necessary.";
            }
            else if (errorMessage.Contains("insufficient disk space", StringComparison.OrdinalIgnoreCase))
            {
                errorTitle = "Insufficient Disk Space";
                errorMessage += "\n\nPlease free up disk space or choose a different destination.";
            }
            else if (errorMessage.Contains("invalid password", StringComparison.OrdinalIgnoreCase))
            {
                errorTitle = "Invalid Password";
                errorMessage += "\n\nPlease verify that you entered the correct password.";
            }
            else if (errorMessage.Contains("corrupted", StringComparison.OrdinalIgnoreCase))
            {
                errorTitle = "File Corruption";
                errorMessage += "\n\nThe file may be damaged or not properly encrypted.";
            }
            else if (errorMessage.Contains("key derivation", StringComparison.OrdinalIgnoreCase))
            {
                errorTitle = "Key Derivation Error";
                errorMessage += "\n\nThere was a problem generating the encryption key.";
            }
            else
            {
                errorTitle = "Operation Failed";
                errorMessage = $"An error occurred during {operation.ToString().ToLower()}: {errorMessage}";
            }

            _dialogService.ShowMessage(errorMessage, errorTitle, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void OnProgressUpdate(FileProcessingStatus update)
    {
        double progress = update.TotalBytes > 0 ? (double)update.ProcessedBytes / update.TotalBytes * 100 : 100;
        double bytesPerSecond = update.ProcessedBytes / update.Elapsed.TotalSeconds;
        TimeSpan eta = update.TotalBytes > 0 && bytesPerSecond > 0
            ? TimeSpan.FromSeconds((update.TotalBytes - update.ProcessedBytes) / bytesPerSecond)
            : TimeSpan.Zero;

        ProgressValue = progress;
        ProgressText = $"Processing: {update.ProcessedFiles}/{update.TotalFiles} files ({progress:F1}%) - ETA: {eta:hh\\:mm\\:ss}";
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

    private async Task UpdatePasswordStrengthAsync(string password, bool isConfirmField)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (isConfirmField)
                ConfirmPasswordStrengthVisibility = Visibility.Hidden;
            else
                PasswordStrengthVisibility = Visibility.Hidden;
            return;
        }

        // Updated to primary-constructor record
        AnalyzePasswordStrengthQuery query = new(password);
        Result<PasswordStrengthResult> result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            PasswordStrengthResult strengthResult = result.Value;
            System.Windows.Media.Brush strengthColor = GetStrengthColor(strengthResult.Strength);

            if (isConfirmField)
            {
                ConfirmPasswordStrengthScore = strengthResult.Score;
                ConfirmPasswordStrengthText = strengthResult.Description;
                ConfirmPasswordStrengthColor = strengthColor;
                ConfirmPasswordStrengthVisibility = Visibility.Visible;
            }
            else
            {
                PasswordStrengthScore = strengthResult.Score;
                PasswordStrengthText = strengthResult.Description;
                PasswordStrengthColor = strengthColor;
                PasswordStrengthVisibility = Visibility.Visible;
            }
        }
        else
        {
            if (isConfirmField)
                ConfirmPasswordStrengthVisibility = Visibility.Hidden;
            else
                PasswordStrengthVisibility = Visibility.Hidden;
        }
    }

    private static System.Windows.Media.Brush GetStrengthColor(PasswordStrength strength)
    {
        return _strengthColorCache.GetValueOrDefault(strength, System.Windows.Media.Brushes.Transparent);
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    #endregion
}