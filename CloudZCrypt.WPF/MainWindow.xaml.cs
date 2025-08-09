using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using System.Diagnostics;
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
        private readonly Stopwatch _stopwatch = new();

        public MainWindow(IEncryptionServiceFactory encryptionServiceFactory)
        {
            InitializeComponent();
            _encryptionServiceFactory = encryptionServiceFactory;

            AlgorithmComboBox.ItemsSource = Enum.GetValues(typeof(EncryptionAlgorithm));
            AlgorithmComboBox.SelectedIndex = 0;
            AlgorithmComboBox.SelectionChanged += AlgorithmComboBox_SelectionChanged;

            UpdateEncryptionService();
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
            dialog.Description = "Select source directory";
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

        private async void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessButton_Click(sender, e, true);
        }

        private async void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessButton_Click(sender, e, false);
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e, bool isEncrypt)
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
                UpdateControlState(true, isEncrypt);
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

                List<string> errors = [];
                int processedFiles = 0;
                long totalBytes = files.Sum(f => new FileInfo(f).Length);
                long processedBytes = 0;

                string src = SourceDirectoryBox.Text;
                string dest = DestinationDirectoryBox.Text;
                string pass = PasswordBox.Password;

                _stopwatch.Restart();

                foreach (string file in files)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    string relativePath = Path.GetRelativePath(src, file);
                    string destinationFilePath = Path.Combine(dest, relativePath);
                    string destinationDirectory = Path.GetDirectoryName(destinationFilePath)!;

                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    bool success = isEncrypt
                        ? await _encryptionService.EncryptFileAsync(file, destinationFilePath, pass)
                        : await _encryptionService.DecryptFileAsync(file, destinationFilePath, pass);

                    if (!success)
                    {
                        errors.Add(file);
                    }

                    processedFiles++;
                    processedBytes += new FileInfo(file).Length;

                    double progress = (double)processedBytes / totalBytes * 100;
                    double bytesPerSecond = processedBytes / _stopwatch.Elapsed.TotalSeconds;
                    TimeSpan estimatedTimeRemaining = TimeSpan.FromSeconds((totalBytes - processedBytes) / bytesPerSecond);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        ProgressBar.Value = progress;
                        ProgressText.Text = $"Processing: {processedFiles}/{files.Length} files" +
                            $" ({progress:F1}%) - ETA: {estimatedTimeRemaining:hh\\:mm\\:ss}";
                    });
                }

                _stopwatch.Stop();
                string operation = isEncrypt ? "Encryption" : "Decryption";

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
                    TimeSpan totalTime = _stopwatch.Elapsed;
                    double speedMBps = totalBytes / (1024.0 * 1024.0) / totalTime.TotalSeconds;

                    MessageBox.Show(
                        $"{operation} completed successfully.\n" +
                        $"Time: {totalTime:hh\\:mm\\:ss}\n" +
                        $"Speed: {speedMBps:F2} MB/s",
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
                UpdateControlState(false, isEncrypt);
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

        private void UpdateControlState(bool processing, bool isEncrypt)
        {
            SourceDirectoryBox.IsEnabled = !processing;
            DestinationDirectoryBox.IsEnabled = !processing;

            SelectSourceDirectoryButton.IsEnabled = !processing;
            SelectDestinationDirectoryButton.IsEnabled = !processing;

            AlgorithmComboBox.IsEnabled = !processing;

            PasswordBox.IsEnabled = !processing;
            ConfirmPasswordBox.IsEnabled = !processing;

            EncryptButton.IsEnabled = !processing || isEncrypt;
            DecryptButton.IsEnabled = !processing || !isEncrypt;
            EncryptButton.Content = processing && isEncrypt ? "Cancel" : "Encrypt";
            DecryptButton.Content = processing && !isEncrypt ? "Cancel" : "Decrypt";

            ProgressBar.Value = processing ? 0 : 100;
            ProgressBar.Visibility = processing ? Visibility.Visible : Visibility.Hidden;
            ProgressText.Visibility = processing ? Visibility.Visible : Visibility.Hidden;
        }
    }
}