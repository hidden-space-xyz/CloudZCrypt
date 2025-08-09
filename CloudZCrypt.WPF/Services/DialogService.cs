using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace CloudZCrypt.WPF.Services
{
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public string? ShowFolderDialog(string description)
        {
            using FolderBrowserDialog dialog = new() { Description = description };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }
}