using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace CloudZCrypt.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IEncryptionServiceFactory _encryptionServiceFactory;
        private IEncryptionService _encryptionService;

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
            dialog.Description = "Select directory to encrypt";
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
            if (string.IsNullOrWhiteSpace(SourceDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(DestinationDirectoryBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                System.Windows.MessageBox.Show("Please complete all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string[] files = System.IO.Directory.GetFiles(SourceDirectoryBox.Text, "*.*", System.IO.SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string relativePath = System.IO.Path.GetRelativePath(SourceDirectoryBox.Text, file);
                    string destinationFilePath = System.IO.Path.Combine(DestinationDirectoryBox.Text, relativePath);
                    string destinationDirectory = System.IO.Path.GetDirectoryName(destinationFilePath);

                    // Create destination directory if it doesn't exist
                    if (!System.IO.Directory.Exists(destinationDirectory))
                    {
                        System.IO.Directory.CreateDirectory(destinationDirectory);
                    }

                    bool result = await _encryptionService.EncryptFileAsync(file, destinationFilePath, PasswordBox.Password);
                    if (!result)
                    {
                        System.Windows.MessageBox.Show($"Error encrypting file: {file}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                System.Windows.MessageBox.Show("Encryption completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}