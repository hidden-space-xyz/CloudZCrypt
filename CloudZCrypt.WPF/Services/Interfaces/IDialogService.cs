using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using System.Windows;

namespace CloudZCrypt.WPF.Services.Interfaces;

public interface IDialogService
{
    void ShowMessage(string message, string title, MessageBoxImage icon);
    void ShowProcessingResult(FileProcessingResult result, EncryptOperation operation, string sourceType);
    void ShowValidationErrors(IEnumerable<string> errors);
    void ShowProcessingWarning(string message, string title);
    bool ShowConfirmation(string message, string title);
    string? ShowFolderDialog(string description);
    string? ShowOpenFileDialog(string title, string filter = "All files (*.*)|*.*");
    string[]? ShowOpenMultipleFilesDialog(string title, string filter = "All files (*.*)|*.*");
    string? ShowSaveFileDialog(string title, string filter = "All files (*.*)|*.*", string defaultFileName = "");
}
