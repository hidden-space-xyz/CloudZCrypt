using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.Views;
using System.Windows;

namespace CloudZCrypt.WPF.Services;

public class DialogService : IDialogService
{
    public void ShowMessage(string message, string title, MessageBoxImage icon)
    {
        Window? owner = System.Windows.Application.Current?.MainWindow;
        MessageDialog dialog = new(message, title, icon, owner);
        dialog.ShowDialog();
    }

    public string? ShowFolderDialog(string description)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new() { Description = description };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }
}
