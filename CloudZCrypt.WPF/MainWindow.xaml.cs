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
        }

        private void AlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEncryptionService();
        }

        private void UpdateEncryptionService()
        {
            if (AlgorithmComboBox.SelectedItem is EncryptionAlgorithm selectedAlgorithm)
            {
                _encryptionService = _encryptionServiceFactory.Create(selectedAlgorithm);
            }
        }

        private void SelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            string description = sender == SelectSourceDirectoryButton
                ? "Select source directory"
                : "Select destination directory";

            using FolderBrowserDialog dialog = new() { Description = description };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                if (sender == SelectSourceDirectoryButton)
                {
                    SourceDirectoryBox.Text = dialog.SelectedPath;
                }
                else
                {
                    DestinationDirectoryBox.Text = dialog.SelectedPath;
                }
            }
        }

        private async void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessFilesAsync(isEncrypt: true);
        }

        private async void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessFilesAsync(isEncrypt: false);
        }

        private async Task ProcessFilesAsync(bool isEncrypt)
        {
            if (_isProcessing)
            {
                _cancellationTokenSource?.Cancel();
                return;
            }

            if (!AreInputsValid())
            {
                return;
            }

            _isProcessing = true;
            UpdateControlState(processing: true, isEncrypt: isEncrypt);
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch.Restart();

            try
            {
                string sourceDir = SourceDirectoryBox.Text;
                string destDir = DestinationDirectoryBox.Text;
                string password = PasswordBox.Password;

                string[] files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    MessageBox.Show("No files found in the source directory.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await ProcessFileBatchAsync(files, sourceDir, destDir, password, isEncrypt, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled by user.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _stopwatch.Stop();
                _isProcessing = false;
                UpdateControlState(processing: false, isEncrypt: isEncrypt);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task ProcessFileBatchAsync(string[] files, string sourceDir, string destDir, string password, bool isEncrypt, CancellationToken cancellationToken)
        {
            List<string> errors = [];
            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            long processedBytes = 0;

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string file = files[i];
                string relativePath = Path.GetRelativePath(sourceDir, file);
                string destinationFilePath = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);

                Task<bool> operation = isEncrypt
                    ? _encryptionService.EncryptFileAsync(file, destinationFilePath, password)
                    : _encryptionService.DecryptFileAsync(file, destinationFilePath, password);

                if (!await operation)
                {
                    errors.Add(file);
                }

                processedBytes += new FileInfo(file).Length;
                UpdateProgress(i + 1, files.Length, processedBytes, totalBytes);
            }

            ShowCompletionMessage(isEncrypt, errors, totalBytes);
        }

        private void UpdateProgress(int processedFiles, int totalFiles, long processedBytes, long totalBytes)
        {
            double progress = totalBytes > 0 ? (double)processedBytes / totalBytes * 100 : 100;
            double bytesPerSecond = processedBytes / _stopwatch.Elapsed.TotalSeconds;
            TimeSpan eta = totalBytes > 0 && bytesPerSecond > 0
                ? TimeSpan.FromSeconds((totalBytes - processedBytes) / bytesPerSecond)
                : TimeSpan.Zero;

            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = progress;
                ProgressText.Text = $"Processing: {processedFiles}/{totalFiles} files ({progress:F1}%) - ETA: {eta:hh\\:mm\\:ss}";
            });
        }

        private void ShowCompletionMessage(bool isEncrypt, List<string> errors, long totalBytes)
        {
            string operation = isEncrypt ? "Encryption" : "Decryption";

            if (errors.Count > 0)
            {
                string errorFiles = string.Join("\n", errors);
                MessageBox.Show($"{operation} completed with {errors.Count} errors.\nFailed files:\n{errorFiles}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                TimeSpan totalTime = _stopwatch.Elapsed;
                double speedMBps = totalBytes > 0 && totalTime.TotalSeconds > 0
                    ? totalBytes / (1024.0 * 1024.0) / totalTime.TotalSeconds
                    : 0;

                MessageBox.Show($"{operation} completed successfully.\nTime: {totalTime:hh\\:mm\\:ss}\nSpeed: {speedMBps:F2} MB/s", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool AreInputsValid()
        {
            if (string.IsNullOrWhiteSpace(SourceDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(DestinationDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please complete all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Path.GetFullPath(SourceDirectoryBox.Text).Equals(Path.GetFullPath(DestinationDirectoryBox.Text), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Source and destination directories cannot be the same.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void UpdateControlState(bool processing, bool isEncrypt)
        {
            System.Windows.Controls.Control[] controlsToDisable = new System.Windows.Controls.Control[]
            {
                SourceDirectoryBox, DestinationDirectoryBox,
                SelectSourceDirectoryButton, SelectDestinationDirectoryButton,
                AlgorithmComboBox, PasswordBox, ConfirmPasswordBox
            };

            foreach (System.Windows.Controls.Control control in controlsToDisable)
            {
                control.IsEnabled = !processing;
            }

            EncryptButton.IsEnabled = !processing || isEncrypt;
            DecryptButton.IsEnabled = !processing || !isEncrypt;
            EncryptButton.Content = processing && isEncrypt ? "Cancel" : "Encrypt";
            DecryptButton.Content = processing && !isEncrypt ? "Cancel" : "Decrypt";

            Visibility progressVisibility = processing ? Visibility.Visible : Visibility.Hidden;
            ProgressBar.Visibility = progressVisibility;
            ProgressText.Visibility = progressVisibility;

            if (!processing)
            {
                ProgressBar.Value = 0;
            }
        }
    }
}