using System.Windows;

namespace CloudZCrypt.WPF.Services
{
    public interface IDialogService
    {
        /// <summary>
        /// Shows a message box with specified content, title, and icon.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The title of the message box</param>
        /// <param name="icon">The icon to display</param>
        void ShowMessage(string message, string title, MessageBoxImage icon);

        /// <summary>
        /// Shows a folder browser dialog and returns the selected path.
        /// </summary>
        /// <param name="description">Description text for the dialog</param>
        /// <returns>Selected folder path or null if cancelled</returns>
        string? ShowFolderDialog(string description);
    }
}