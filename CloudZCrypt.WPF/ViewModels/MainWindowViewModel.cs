using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.WPF.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CloudZCrypt.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    #region Private Fields

    private readonly IDialogService _dialogService;
    private readonly IFileEncryptionApplicationService _fileEncryptionApplicationService;
    private readonly IPasswordApplicationService _passwordApplicationService;

    private CancellationTokenSource? _cancellationTokenSource;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private string _sourceDirectory = string.Empty;

    [ObservableProperty]
    private string _destinationDirectory = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible = false;

    [ObservableProperty]
    private EncryptionAlgorithm _selectedEncryptionAlgorithm;

    [ObservableProperty]
    private KeyDerivationAlgorithm _selectedKeyDerivationAlgorithm;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private Visibility _progressVisibility = Visibility.Hidden;

    [ObservableProperty]
    private string _encryptButtonText = "Encrypt";

    [ObservableProperty]
    private string _decryptButtonText = "Decrypt";

    [ObservableProperty]
    private bool _canEncrypt = true;

    [ObservableProperty]
    private bool _canDecrypt = true;

    [ObservableProperty]
    private bool _areControlsEnabled = true;

    // Password strength properties
    [ObservableProperty]
    private double _passwordStrengthScore;

    [ObservableProperty]
    private string _passwordStrengthText = string.Empty;

    [ObservableProperty]
    private System.Windows.Media.Brush _passwordStrengthColor = System.Windows.Media.Brushes.Transparent;

    [ObservableProperty]
    private Visibility _passwordStrengthVisibility = Visibility.Hidden;

    // Confirm Password strength properties
    [ObservableProperty]
    private double _confirmPasswordStrengthScore;

    [ObservableProperty]
    private string _confirmPasswordStrengthText = string.Empty;

    [ObservableProperty]
    private System.Windows.Media.Brush _confirmPasswordStrengthColor = System.Windows.Media.Brushes.Transparent;

    [ObservableProperty]
    private Visibility _confirmPasswordStrengthVisibility = Visibility.Hidden;

    #endregion

    #region Collections

    public ObservableCollection<EncryptionAlgorithm> AvailableEncryptionAlgorithms { get; }
    public ObservableCollection<KeyDerivationAlgorithm> AvailableKeyDerivationAlgorithms { get; }

    #endregion

    #region Constructor

    public MainWindowViewModel(
        IDialogService dialogService,
        IFileEncryptionApplicationService fileEncryptionService,
        IPasswordApplicationService passwordApplicationService)
    {
        _dialogService = dialogService;
        _fileEncryptionApplicationService = fileEncryptionService;
        _passwordApplicationService = passwordApplicationService;

        AvailableEncryptionAlgorithms = new ObservableCollection<EncryptionAlgorithm>(Enum.GetValues<EncryptionAlgorithm>());
        AvailableKeyDerivationAlgorithms = new ObservableCollection<KeyDerivationAlgorithm>(Enum.GetValues<KeyDerivationAlgorithm>());
        SelectedEncryptionAlgorithm = EncryptionAlgorithm.Aes; // Default algorithm
        SelectedKeyDerivationAlgorithm = KeyDerivationAlgorithm.PBKDF2; // Default KDF algorithm

#if DEBUG
        SourceDirectory = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        DestinationDirectory = @"D:\WorkSpace\EncryptionTest\Result";
#endif
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GenerateStrongPassword()
    {
        PasswordCompositionOptions passwordCompositionOptions =
            PasswordCompositionOptions.IncludeLowercase
            | PasswordCompositionOptions.IncludeUppercase
            | PasswordCompositionOptions.IncludeNumbers
            | PasswordCompositionOptions.IncludeSpecialCharacters;

        Result<string> result = await _passwordApplicationService.GeneratePasswordAsync(128, passwordCompositionOptions);

        if (result.IsSuccess)
        {
            Password = result.Value;
            ConfirmPassword = result.Value;

            // Copy to clipboard
            try
            {
                System.Windows.Clipboard.SetText(result.Value);
            }
            catch
            {
                // Silently fail if clipboard access is not available
            }
        }
        else
        {
            _dialogService.ShowMessage($"Failed to generate password: {string.Join(", ", result.Errors)}", "Error", MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SelectSourceDirectory()
    {
        string? selectedPath = _dialogService.ShowFolderDialog("Select source directory");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            SourceDirectory = selectedPath;
        }
    }

    [RelayCommand]
    private void SelectDestinationDirectory()
    {
        string? selectedPath = _dialogService.ShowFolderDialog("Select destination directory");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            DestinationDirectory = selectedPath;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteEncrypt))]
    private async Task EncryptAsync()
    {
        await ProcessFilesAsync(EncryptOperation.Encrypt);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDecrypt))]
    private async Task DecryptAsync()
    {
        await ProcessFilesAsync(EncryptOperation.Decrypt);
    }

    #endregion

    #region Command CanExecute Methods

    private bool CanExecuteEncrypt()
    {
        return !IsProcessing || CanEncrypt;
    }

    private bool CanExecuteDecrypt()
    {
        return !IsProcessing || CanDecrypt;
    }

    #endregion

    #region Property Change Handlers

    partial void OnIsProcessingChanged(bool value)
    {
        UpdateControlState();
        EncryptCommand.NotifyCanExecuteChanged();
        DecryptCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        UpdatePasswordStrength(value);
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        UpdateConfirmPasswordStrength(value);
    }

    #endregion

    #region Private Methods

    private async Task ProcessFilesAsync(EncryptOperation encryptOperation)
    {
        if (IsProcessing)
        {
            _cancellationTokenSource?.Cancel();
            return;
        }

        if (!AreInputsValid())
            return;

        IsProcessing = true;
        _cancellationTokenSource = new CancellationTokenSource();

        UpdateButtonStates(encryptOperation);

        try
        {
            Progress<FileProcessingStatus> progress = new(OnProgressUpdate);

            Result<FileProcessingResult> result = encryptOperation == EncryptOperation.Encrypt
                ? await _fileEncryptionApplicationService.EncryptFilesAsync(
                    SourceDirectory,
                    DestinationDirectory,
                    Password,
                    SelectedEncryptionAlgorithm,
                    SelectedKeyDerivationAlgorithm,
                    progress,
                    _cancellationTokenSource.Token)
                : await _fileEncryptionApplicationService.DecryptFilesAsync(
                    SourceDirectory,
                    DestinationDirectory,
                    Password,
                    SelectedEncryptionAlgorithm,
                    SelectedKeyDerivationAlgorithm,
                    progress,
                    _cancellationTokenSource.Token);

            if (result.IsSuccess)
            {
                ShowCompletionMessage(encryptOperation, result.Value);
            }
            else
            {
                _dialogService.ShowMessage($"Operation failed: {string.Join(", ", result.Errors)}", "Error", MessageBoxImage.Error);
            }
        }
        catch (OperationCanceledException)
        {
            _dialogService.ShowMessage("Operation cancelled by user.", "Information", MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            ResetButtonStates();
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

    private void ShowCompletionMessage(EncryptOperation encryptOperation, FileProcessingResult result)
    {
        string operation = encryptOperation == EncryptOperation.Encrypt ? "Encryption" : "Decryption";

        if (!result.IsSuccess && result.Errors.Count > 0)
        {
            string errorFiles = string.Join("\n", result.Errors);
            _dialogService.ShowMessage(
                $"{operation} completed with {result.Errors.Count} errors.\nFailed files:\n{errorFiles}",
                "Warning",
                MessageBoxImage.Warning);
        }
        else
        {
            double speedMBps = result.TotalBytes > 0 && result.ElapsedTime.TotalSeconds > 0
                ? result.TotalBytes / (1024.0 * 1024.0) / result.ElapsedTime.TotalSeconds
                : 0;

            _dialogService.ShowMessage(
                $"{operation} completed successfully.\nTime: {result.ElapsedTime:hh\\:mm\\:ss}\nSpeed: {speedMBps:F2} MB/s",
                "Success",
                MessageBoxImage.Information);
        }
    }

    private bool AreInputsValid()
    {
        if (string.IsNullOrWhiteSpace(SourceDirectory) ||
            string.IsNullOrWhiteSpace(DestinationDirectory) ||
            string.IsNullOrWhiteSpace(Password))
        {
            _dialogService.ShowMessage("Please complete all fields.", "Error", MessageBoxImage.Error);
            return false;
        }

        if (Password != ConfirmPassword)
        {
            _dialogService.ShowMessage("Passwords do not match.", "Error", MessageBoxImage.Error);
            return false;
        }

        if (Path.GetFullPath(SourceDirectory).Equals(Path.GetFullPath(DestinationDirectory), StringComparison.OrdinalIgnoreCase))
        {
            _dialogService.ShowMessage("Source and destination directories cannot be the same.", "Error", MessageBoxImage.Error);
            return false;
        }

        if (DestinationDirectory.StartsWith(SourceDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _dialogService.ShowMessage("Destination directory cannot be inside the source directory.", "Error", MessageBoxImage.Error);
            return false;
        }

        if (!Directory.Exists(SourceDirectory))
        {
            _dialogService.ShowMessage("Source directory does not exist.", "Error", MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private void UpdateControlState()
    {
        AreControlsEnabled = !IsProcessing;
        ProgressVisibility = IsProcessing ? Visibility.Visible : Visibility.Hidden;

        if (!IsProcessing)
        {
            ProgressValue = 0;
            ProgressText = string.Empty;
        }
    }

    private void UpdateButtonStates(EncryptOperation encryptOperation)
    {
        if (encryptOperation == EncryptOperation.Encrypt)
        {
            EncryptButtonText = "Encrypt";
            DecryptButtonText = "Cancel";
        }
        else
        {
            EncryptButtonText = "Cancel";
            DecryptButtonText = "Decrypt";
        }
    }

    private void ResetButtonStates()
    {
        EncryptButtonText = "Encrypt";
        DecryptButtonText = "Decrypt";
    }

    private async void UpdatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            PasswordStrengthVisibility = Visibility.Hidden;
            return;
        }

        Application.Common.Models.Result<PasswordStrengthResult> result = await _passwordApplicationService.AnalyzePasswordStrengthAsync(password);

        if (result.IsSuccess)
        {
            PasswordStrengthScore = result.Value.Score;
            PasswordStrengthText = result.Value.Description;
            PasswordStrengthColor = GetStrengthColor(result.Value.Strength);
            PasswordStrengthVisibility = Visibility.Visible;
        }
        else
        {
            PasswordStrengthVisibility = Visibility.Hidden;
        }
    }

    private async void UpdateConfirmPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            ConfirmPasswordStrengthVisibility = Visibility.Hidden;
            return;
        }

        Application.Common.Models.Result<PasswordStrengthResult> result = await _passwordApplicationService.AnalyzePasswordStrengthAsync(password);

        if (result.IsSuccess)
        {
            ConfirmPasswordStrengthScore = result.Value.Score;
            ConfirmPasswordStrengthText = result.Value.Description;
            ConfirmPasswordStrengthColor = GetStrengthColor(result.Value.Strength);
            ConfirmPasswordStrengthVisibility = Visibility.Visible;
        }
        else
        {
            ConfirmPasswordStrengthVisibility = Visibility.Hidden;
        }
    }

    private static System.Windows.Media.Brush GetStrengthColor(PasswordStrength strength)
    {
        return strength switch
        {
            PasswordStrength.VeryWeak => new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),   // Red
            PasswordStrength.Weak => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),       // Orange/Yellow
            PasswordStrength.Fair => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),       // Orange/Yellow
            PasswordStrength.Good => new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),       // Green
            PasswordStrength.Strong => new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 135, 84)),     // Dark Green
            _ => System.Windows.Media.Brushes.Transparent
        };
    }

    #endregion
}