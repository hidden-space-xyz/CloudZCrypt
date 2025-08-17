using System.Windows;

namespace CloudZCrypt.WPF.Services.Interfaces;

public interface IDialogService
{
    void ShowMessage(string message, string title, MessageBoxImage icon);
    string? ShowFolderDialog(string description);
}
