using CloudZCrypt.Application.DataTransferObjects.Files;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.UseCases;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories.Interfaces;
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
    private readonly IPasswordService _passwordStrengthService;

    private readonly EncryptFileUseCase _encryptFileUseCase;
    
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
    private Visibility _passwordStrengthVisibility = Visibility.Collapsed;

    // Confirm Password strength properties
    [ObservableProperty]
    private double _confirmPasswordStrengthScore;

    [ObservableProperty]
    private string _confirmPasswordStrengthText = string.Empty;

    [ObservableProperty]
    private System.Windows.Media.Brush _confirmPasswordStrengthColor = System.Windows.Media.Brushes.Transparent;

    [ObservableProperty]
    private Visibility _confirmPasswordStrengthVisibility = Visibility.Collapsed;

    #endregion

    #region Collections

    public ObservableCollection<EncryptionAlgorithm> AvailableEncryptionAlgorithms { get; }
    public ObservableCollection<KeyDerivationAlgorithm> AvailableKeyDerivationAlgorithms { get; }

    #endregion

    #region Constructor

    public MainWindowViewModel(
        IDialogService dialogService,
        IPasswordService passwordStrengthService,
        EncryptFileUseCase encryptFileUseCase)
    {
        _dialogService = dialogService;
        _passwordStrengthService = passwordStrengthService;

        _encryptFileUseCase = encryptFileUseCase;

        AvailableEncryptionAlgorithms = new ObservableCollection<EncryptionAlgorithm>(Enum.GetValues<EncryptionAlgorithm>());
        AvailableKeyDerivationAlgorithms = new ObservableCollection<KeyDerivationAlgorithm>(Enum.GetValues<KeyDerivationAlgorithm>());
        SelectedEncryptionAlgorithm = EncryptionAlgorithm.Aes; // Default algorithm
        SelectedKeyDerivationAlgorithm = KeyDerivationAlgorithm.Argon2id; // Default KDF algorithm

#if DEBUG
        SourceDirectory = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        DestinationDirectory = @"D:\WorkSpace\EncryptionTest\Result";
        Password = "TestPassword123";
        ConfirmPassword = "TestPassword123";
        SelectedEncryptionAlgorithm = EncryptionAlgorithm.Aes;
        SelectedKeyDerivationAlgorithm = KeyDerivationAlgorithm.PBKDF2; // For faster testing
#endif
    }

    #endregion

    #region Commands

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
            _cancellationTokenSource?.CancelAsync();
            return;
        }

        if (!AreInputsValid())
            return;

        IsProcessing = true;
        _cancellationTokenSource = new CancellationTokenSource();

        UpdateButtonStates(encryptOperation);

        try
        {
            FileProcessingRequest request = new(
                SourceDirectory,
                DestinationDirectory,
                Password,
                encryptOperation,
                SelectedEncryptionAlgorithm,
                SelectedKeyDerivationAlgorithm);

            Progress<FileProcessingStatus> progress = new(OnProgressUpdate);

            FileProcessingResult result =
                await _encryptFileUseCase.EncryptFilesAsync(request, progress, _cancellationTokenSource.Token);

            ShowCompletionMessage(encryptOperation, result);
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

    private void UpdatePasswordStrength(string password)
    {
        PasswordStrengthResult result = _passwordStrengthService.EvaluatePasswordStrength(password);

        PasswordStrengthScore = result.Score;
        PasswordStrengthText = result.Description;
        PasswordStrengthColor = GetStrengthColor(result.Strength);
        PasswordStrengthVisibility = string.IsNullOrEmpty(password) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateConfirmPasswordStrength(string password)
    {
        PasswordStrengthResult result = _passwordStrengthService.EvaluatePasswordStrength(password);

        ConfirmPasswordStrengthScore = result.Score;
        ConfirmPasswordStrengthText = result.Description;
        ConfirmPasswordStrengthColor = GetStrengthColor(result.Strength);
        ConfirmPasswordStrengthVisibility = string.IsNullOrEmpty(password) ? Visibility.Collapsed : Visibility.Visible;
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