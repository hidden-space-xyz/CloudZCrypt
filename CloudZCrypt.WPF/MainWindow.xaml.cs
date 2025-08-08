using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace CloudZCrypt.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IEncryptionServiceFactory _encryptionServiceFactory;
        private IEncryptionService _encryptionService;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isProcessing;

        public MainWindow(IEncryptionServiceFactory encryptionServiceFactory)
        {
            InitializeComponent();
            _encryptionServiceFactory = encryptionServiceFactory;

            AlgorithmComboBox.ItemsSource = Enum.GetValues(typeof(EncryptionAlgorithm));
            AlgorithmComboBox.SelectedIndex = 0;
            AlgorithmComboBox.SelectionChanged += AlgorithmComboBox_SelectionChanged;

            EncryptMode.Checked += Mode_Changed;
            DecryptMode.Checked += Mode_Changed;

            UpdateEncryptionService();
            UpdateUIState();
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            ProcessButton.Content = EncryptMode.IsChecked == true ? "Encrypt" : "Decrypt";
        }

        private void AlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEncryptionService();
        }

        private void UpdateEncryptionService()
        {
            EncryptionAlgorithm selectedAlgorithm = (EncryptionAlgorithm)AlgorithmComboBox.SelectedItem;
            _encryptionService = _encryptionServiceFactory.Create(selectedAlgorithm);
        }

        private void SelectSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new();
            dialog.Description = EncryptMode.IsChecked == true
                ? "Select directory to encrypt"
                : "Select directory with encrypted files";
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                SourceDirectoryBox.Text = dialog.SelectedPath;
            }
        }

        private void SelectDestinationDirectory_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new();
            dialog.Description = "Select destination directory";
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                DestinationDirectoryBox.Text = dialog.SelectedPath;
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                _cancellationTokenSource?.Cancel();
                return;
            }

            if (!ValidateInput())
                return;

            try
            {
                _isProcessing = true;
                UpdateControlState(true);
                _cancellationTokenSource = new CancellationTokenSource();

                string[] files = Directory.GetFiles(SourceDirectoryBox.Text, "*.*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    MessageBox.Show(
                        "No files found in the source directory.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                ConcurrentBag<string> errors = [];
                int processedFiles = 0;

                string src = SourceDirectoryBox.Text;
                string dest = DestinationDirectoryBox.Text;
                string pass = PasswordBox.Password;
                bool? encryptMode = EncryptMode.IsChecked;

                await Task.Run(async () =>
                {
                    ParallelOptions options = new()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = _cancellationTokenSource.Token
                    };

                    await Parallel.ForEachAsync(files, options, async (file, token) =>
                    {
                        string relativePath = Path.GetRelativePath(src, file);
                        string destinationFilePath = Path.Combine(dest, relativePath);
                        string destinationDirectory = Path.GetDirectoryName(destinationFilePath)!;

                        if (!Directory.Exists(destinationDirectory))
                        {
                            Directory.CreateDirectory(destinationDirectory);
                        }

                        bool success = encryptMode == true
                            ? await _encryptionService.EncryptFileAsync(file, destinationFilePath, pass)
                            : await _encryptionService.DecryptFileAsync(file, destinationFilePath, pass);

                        if (!success)
                        {
                            errors.Add(file);
                        }

                        int current = Interlocked.Increment(ref processedFiles);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            ProgressBar.Value = (double)current / files.Length * 100;
                            ProgressText.Text = $"Processing: {current}/{files.Length} files completed";
                        });
                    });
                });

                string operation = encryptMode == true ? "Encryption" : "Decryption";
                if (errors.Count > 0)
                {
                    MessageBox.Show(
                        $"{operation} completed with {errors.Count} errors.\nFailed files:\n{string.Join("\n", errors)}",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"{operation} completed successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    "Operation cancelled by user.",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isProcessing = false;
                UpdateControlState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(SourceDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(DestinationDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show(
                    "Please complete all fields.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show(
                    "Passwords do not match.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (Path.GetFullPath(SourceDirectoryBox.Text) == Path.GetFullPath(DestinationDirectoryBox.Text))
            {
                MessageBox.Show(
                    "Source and destination directories cannot be the same.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void UpdateControlState(bool processing)
        {
            ProgressBar.Value = processing ? 0 : 100;
            ProgressText.Text = processing ? "Starting..." : "Ready";

            AlgorithmComboBox.IsEnabled = !processing;
            SourceDirectoryBox.IsEnabled = !processing;
            DestinationDirectoryBox.IsEnabled = !processing;
            PasswordBox.IsEnabled = !processing;
            ConfirmPasswordBox.IsEnabled = !processing;
            EncryptMode.IsEnabled = !processing;
            DecryptMode.IsEnabled = !processing;

            ProcessButton.Content = processing ? "Cancel" : (EncryptMode.IsChecked == true ? "Encrypt" : "Decrypt");
            CancelButton.IsEnabled = processing;
        }
    }
}