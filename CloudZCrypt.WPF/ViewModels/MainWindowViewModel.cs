using CloudZCrypt.Application.Commands;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Queries;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Extensions;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace CloudZCrypt.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    #region Private Fields

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IFileSystemService _fileSystemService;

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

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private string _sourceDirectory = string.Empty;

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
    private string _encryptButtonText = "Create Vault";

    [ObservableProperty]
    private bool _areControlsEnabled = true;

    [ObservableProperty]
    private bool _areMountControlsEnabled = true;

    [ObservableProperty]
    private MountPoint _selectedMountPoint = MountPoint.Z;

    [ObservableProperty]
    private string _encryptedVaultPath = string.Empty;

    [ObservableProperty]
    private bool _isVaultMounted = false;

    [ObservableProperty]
    private string _mountButtonText = "Mount Vault";

    [ObservableProperty]
    private string _currentMountStatus = string.Empty;

    [ObservableProperty]
    private SolidColorBrush _mountStatusColor = new(Colors.Gray);

    [ObservableProperty]
    private Visibility _mountStatusVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private double _passwordStrengthScore;

    [ObservableProperty]
    private string _passwordStrengthText = string.Empty;

    [ObservableProperty]
    private System.Windows.Media.Brush _passwordStrengthColor = System.Windows.Media.Brushes.Transparent;

    [ObservableProperty]
    private Visibility _passwordStrengthVisibility = Visibility.Hidden;

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
    public ObservableCollection<MountPoint> AvailableMountPoints { get; }

    #endregion

    #region Constructor

    public MainWindowViewModel(
        IDialogService dialogService,
        IMediator mediator,
        IFileSystemService fileSystemService)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _fileSystemService = fileSystemService;

        AvailableEncryptionAlgorithms = new ObservableCollection<EncryptionAlgorithm>(Enum.GetValues<EncryptionAlgorithm>());
        AvailableKeyDerivationAlgorithms = new ObservableCollection<KeyDerivationAlgorithm>(Enum.GetValues<KeyDerivationAlgorithm>());
        AvailableMountPoints = [];
        SelectedEncryptionAlgorithm = EncryptionAlgorithm.Aes;
        SelectedKeyDerivationAlgorithm = KeyDerivationAlgorithm.PBKDF2;

        // Initialize available mount points and select the first available one
        RefreshAvailableMountPoints();

#if DEBUG
        SourceDirectory = @"D:\WorkSpace\EncryptionTest\ToEncrypt";
        EncryptedVaultPath = @"D:\WorkSpace\EncryptionTest\Vault";
#endif

        UpdateVaultMountStatus();
        UpdateControlState(); // Ensure controls are in correct state
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GenerateStrongPassword()
    {
        GeneratePasswordQuery query = new()
        {
            Length = 128,
            IncludeUppercase = true,
            IncludeLowercase = true,
            IncludeNumbers = true,
            IncludeSpecialCharacters = true,
            ExcludeSimilarCharacters = false
        };

        Result<string> result = await _mediator.Send(query);

        if (result.IsSuccess)
        {
            Password = result.Value;
            ConfirmPassword = result.Value;

            try
            {
                System.Windows.Clipboard.SetText(result.Value);
            }
            catch
            {
                // Ignore clipboard errors
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
    private void SelectEncryptedVaultPath()
    {
        string? selectedPath = _dialogService.ShowFolderDialog("Select encrypted vault directory");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            EncryptedVaultPath = selectedPath;
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

    [RelayCommand(CanExecute = nameof(CanExecuteCreateVault))]
    private async Task CreateVaultAsync()
    {
        if (!AreCreateVaultInputsValid())
            return;

        IsProcessing = true;
        EncryptButtonText = "Creating Vault...";

        try
        {
            Progress<FileProcessingStatus> progress = new(OnProgressUpdate);
            _cancellationTokenSource = new CancellationTokenSource();

            Directory.CreateDirectory(EncryptedVaultPath);

            EncryptFilesCommand command = new()
            {
                SourceDirectory = SourceDirectory,
                DestinationDirectory = EncryptedVaultPath,
                Password = Password,
                EncryptionAlgorithm = SelectedEncryptionAlgorithm,
                KeyDerivationAlgorithm = SelectedKeyDerivationAlgorithm,
                EncryptOperation = EncryptOperation.Encrypt,
                Progress = progress
            };

            Result<FileProcessingResult> result = await _mediator.Send(command, _cancellationTokenSource.Token);

            if (result.IsSuccess)
            {
                _dialogService.ShowMessage(
                    $"Vault created successfully!\nFiles encrypted from: {SourceDirectory}\nVault location: {EncryptedVaultPath}",
                    "Success",
                    MessageBoxImage.Information);
            }
            else
            {
                _dialogService.ShowMessage($"Failed to create vault: {string.Join(", ", result.Errors)}", "Error", MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Error creating vault: {ex.Message}", "Error", MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            EncryptButtonText = "Create Vault";
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteMountUnmount))]
    private async Task MountUnmountVaultAsync()
    {
        if (IsVaultMounted)
        {
            await UnmountVaultAsync();
        }
        else
        {
            await MountVaultAsync();
        }
    }

    [RelayCommand]
    private async Task OpenMountPoint()
    {
        if (IsVaultMounted && Directory.Exists(SelectedMountPoint.ToDriveString()))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", SelectedMountPoint.ToDriveString());
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Failed to open mount point: {ex.Message}", "Error", MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Command CanExecute Methods

    private bool CanExecuteCreateVault()
    {
        return !IsProcessing && !IsVaultMounted; // Prevent creating vault when another is mounted
    }

    private bool CanExecuteMountUnmount()
    {
        // Allow unmounting even without password, but require password for mounting
        if (IsVaultMounted)
        {
            return !IsProcessing; // Can always unmount if not processing
        }
        else
        {
            return !IsProcessing && !string.IsNullOrWhiteSpace(EncryptedVaultPath) && !string.IsNullOrWhiteSpace(Password);
        }
    }

    #endregion

    #region Property Change Handlers

    partial void OnIsProcessingChanged(bool value)
    {
        UpdateControlState();
        CreateVaultCommand.NotifyCanExecuteChanged();
        MountUnmountVaultCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value)
    {
        _ = UpdatePasswordStrengthAsync(value, isConfirmField: false);
        MountUnmountVaultCommand.NotifyCanExecuteChanged();
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        _ = UpdatePasswordStrengthAsync(value, isConfirmField: true);
    }

    partial void OnEncryptedVaultPathChanged(string value)
    {
        MountUnmountVaultCommand.NotifyCanExecuteChanged();
        UpdateVaultMountStatus();
    }

    partial void OnSelectedMountPointChanged(MountPoint value)
    {
        UpdateVaultMountStatus();
    }

    partial void OnIsVaultMountedChanged(bool value)
    {
        MountUnmountVaultCommand.NotifyCanExecuteChanged();
        UpdateControlState(); // Update control state when mount status changes
    }

    #endregion

    #region Public Methods
    public async Task EmergencyCleanupAsync()
    {
        try
        {
            if (IsVaultMounted)
            {
                await _fileSystemService.UnmountVolumeAsync(SelectedMountPoint.ToDriveString());
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    #endregion

    #region Private Methods

    private void RefreshAvailableMountPoints()
    {
        IEnumerable<MountPoint> availablePoints = _fileSystemService.GetAvailableMountPoints();

        AvailableMountPoints.Clear();
        foreach (MountPoint point in availablePoints)
        {
            AvailableMountPoints.Add(point);
        }

        // Set default selection to first available mount point, or Z if none available
        if (AvailableMountPoints.Any())
        {
            SelectedMountPoint = AvailableMountPoints.First();
        }
        else
        {
            SelectedMountPoint = MountPoint.Z; // Fallback
        }
    }

    private async Task MountVaultAsync()
    {
        if (!AreMountInputsValid())
            return;

        IsProcessing = true;
        MountButtonText = "Mounting...";
        UpdateMountStatus("Mounting...", Colors.Orange);

        try
        {
            bool result = await _fileSystemService.MountVolumeAsync(
                EncryptedVaultPath,
                SelectedMountPoint.ToDriveString(),
                Password,
                SelectedEncryptionAlgorithm,
                SelectedKeyDerivationAlgorithm);

            if (result)
            {
                IsVaultMounted = true;
                MountButtonText = "Unmount Vault";
                UpdateMountStatus($"Mounted at {SelectedMountPoint.ToDriveString()}", Colors.Green);

                _dialogService.ShowMessage(
                    $"Vault mounted successfully at {SelectedMountPoint.ToDriveString()}",
                    "Success",
                    MessageBoxImage.Information);
            }
            else
            {
                UpdateMountStatus("Mount failed", Colors.Red);
                _dialogService.ShowMessage("Failed to mount vault", "Error", MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateMountStatus("Mount error", Colors.Red);
            _dialogService.ShowMessage($"Error mounting vault: {ex.Message}", "Error", MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            if (!IsVaultMounted)
            {
                MountButtonText = "Mount Vault";
                UpdateMountStatus("Not mounted", Colors.Gray);
            }
        }
    }

    private async Task UnmountVaultAsync()
    {
        IsProcessing = true;
        MountButtonText = "Unmounting...";
        UpdateMountStatus("Unmounting...", Colors.Orange);

        try
        {
            bool result = await _fileSystemService.UnmountVolumeAsync(SelectedMountPoint.ToDriveString());

            if (result)
            {
                IsVaultMounted = false;
                MountButtonText = "Mount Vault";
                UpdateMountStatus("Not mounted", Colors.Gray);

                // Refresh available mount points to include the now-freed drive letter
                RefreshAvailableMountPoints();

                _dialogService.ShowMessage(
                    "Vault unmounted successfully",
                    "Success",
                    MessageBoxImage.Information);
            }
            else
            {
                UpdateMountStatus($"Still mounted at {SelectedMountPoint.ToDriveString()}", Colors.Red);
                _dialogService.ShowMessage("Failed to unmount vault", "Error", MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateMountStatus($"Still mounted at {SelectedMountPoint.ToDriveString()}", Colors.Red);
            _dialogService.ShowMessage($"Error unmounting vault: {ex.Message}", "Error", MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
            if (IsVaultMounted)
                MountButtonText = "Unmount Vault";
        }
    }

    private void UpdateMountStatus(string status, System.Windows.Media.Color color)
    {
        CurrentMountStatus = status;
        MountStatusColor = new SolidColorBrush(color);
        MountStatusVisibility = Visibility.Visible;
    }

    private void UpdateVaultMountStatus()
    {
        string mountPointString = SelectedMountPoint.ToDriveString();
        bool isMounted = Directory.Exists(mountPointString) && !IsDirectoryEmpty(mountPointString);

        IsVaultMounted = isMounted;
        MountButtonText = IsVaultMounted ? "Unmount Vault" : "Mount Vault";

        if (IsVaultMounted)
        {
            UpdateMountStatus($"Mounted at {mountPointString}", Colors.Green);
        }
        else
        {
            UpdateMountStatus("Not mounted", Colors.Gray);
        }
    }

    private static bool IsDirectoryEmpty(string path)
    {
        try
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
        catch
        {
            return true;
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

    private bool AreCreateVaultInputsValid()
    {
        // Basic UI validation - let domain handle detailed validation
        if (string.IsNullOrWhiteSpace(SourceDirectory) ||
            string.IsNullOrWhiteSpace(EncryptedVaultPath) ||
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

        return true;
    }

    private bool AreMountInputsValid()
    {
        // Basic UI validation - let domain handle detailed validation
        if (string.IsNullOrWhiteSpace(EncryptedVaultPath) ||
            string.IsNullOrWhiteSpace(Password))
        {
            _dialogService.ShowMessage("Please complete all fields.", "Error", MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private void UpdateControlState()
    {
        AreControlsEnabled = !IsProcessing;
        AreMountControlsEnabled = !IsProcessing && !IsVaultMounted; // Disable mount controls when vault is mounted
        ProgressVisibility = IsProcessing ? Visibility.Visible : Visibility.Hidden;

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

        AnalyzePasswordStrengthQuery query = new() { Password = password };
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
}
