using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.Views;
using System.Text;
using System.Windows;

namespace CloudZCrypt.WPF.Services;

/// <summary>
/// Provides a concrete implementation of <see cref="IDialogService"/> for displaying
/// application dialogs (confirmation, messages, validation feedback, file selection,
/// and operation summaries) within the WPF client.
/// </summary>
/// <remarks>
/// This service centralizes dialog creation to promote consistency in user messaging and
/// to simplify view model logic by abstracting UI-specific concerns. It leverages custom
/// WPF dialog windows (e.g., <see cref="ConfirmationDialog"/>, <see cref="MessageDialog"/>)
/// and standard Win32 dialogs for file system interactions.
/// </remarks>
public class DialogService : IDialogService
{
    /// <summary>
    /// Displays a modal confirmation dialog prompting the user to accept or cancel an action.
    /// </summary>
    /// <param name="message">The confirmation message to present to the user. Should be concise.</param>
    /// <param name="title">The window title describing the context of the confirmation.</param>
    /// <returns>true if the user confirms (e.g., OK/Yes); otherwise, false.</returns>
    public bool ShowConfirmation(string message, string title)
    {
        Window? owner = System.Windows.Application.Current?.MainWindow;
        ConfirmationDialog dialog = new(message, title, owner);
        return dialog.ShowDialog() == true;
    }

    /// <summary>
    /// Displays a general purpose modal message dialog with a specified icon.
    /// </summary>
    /// <param name="message">The message body to display. May contain multiple lines.</param>
    /// <param name="title">The caption/title of the dialog window.</param>
    /// <param name="icon">The visual icon indicating the message category (information, warning, error, etc.).</param>
    public void ShowMessage(string message, string title, MessageBoxImage icon)
    {
        Window? owner = System.Windows.Application.Current?.MainWindow;
        MessageDialog dialog = new(message, title, icon, owner);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Displays a warning dialog highlighting a processing-related issue that does not prevent continuation.
    /// </summary>
    /// <param name="message">A human-readable description of the warning condition.</param>
    /// <param name="title">The dialog title clarifying the context of the warning.</param>
    public void ShowProcessingWarning(string message, string title)
    {
        ShowMessage($"⚠️ {message}", title, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Displays a formatted list of validation errors to the user in a modal dialog.
    /// </summary>
    /// <param name="errors">A sequence of validation error messages. Each entry is rendered as a bullet point.</param>
    public void ShowValidationErrors(IEnumerable<string> errors)
    {
        StringBuilder message = new();
        message.AppendLine("⚠️ Please correct the following issues before proceeding:");
        message.AppendLine();

        foreach (string error in errors)
        {
            message.AppendLine($"• {error}");
        }

        ShowMessage(message.ToString(), "Validation Error", MessageBoxImage.Warning);
    }

    /// <summary>
    /// Displays a detailed summary dialog describing the outcome of a batch encryption or decryption operation.
    /// </summary>
    /// <param name="result">The file processing result containing success status, statistics, and any errors.</param>
    /// <param name="operation">The cryptographic operation performed (encrypt or decrypt).</param>
    /// <param name="sourceType">A textual description of the source context (e.g., "Folder", "File"). Currently informational.</param>
    public void ShowProcessingResult(FileProcessingResult result, EncryptOperation operation, string sourceType)
    {
        string operationText = operation == EncryptOperation.Encrypt ? "Encryption" : "Decryption";
        string title = $"{operationText} Complete";
        MessageBoxImage icon =
            result.IsSuccess ? MessageBoxImage.Information
            : result.IsPartialSuccess ? MessageBoxImage.Warning
            : MessageBoxImage.Error;

        StringBuilder message = new();

        if (result.IsSuccess)
        {
            message.AppendLine($"✅ {operationText} completed successfully!");
        }
        else if (result.IsPartialSuccess)
        {
            message.AppendLine($"⚠️ {operationText} completed with some errors.");
        }
        else
        {
            message.AppendLine($"❌ {operationText} failed.");
        }

        message.AppendLine();
        message.AppendLine("📊 Statistics:");
        message.AppendLine($"   • Files processed: {result.ProcessedFiles:N0} of {result.TotalFiles:N0}");

        if (result.TotalFiles > 1)
        {
            message.AppendLine($"   • Success rate: {result.SuccessRate:P1}");
        }

        message.AppendLine($"   • Total size: {FormatBytes(result.TotalBytes)}");
        message.AppendLine($"   • Time elapsed: {FormatDuration(result.ElapsedTime)}");

        if (result.ElapsedTime.TotalSeconds > 0)
        {
            message.AppendLine($"   • Processing speed: {FormatBytes((long)result.BytesPerSecond)}/s");

            if (result.TotalFiles > 1)
            {
                message.AppendLine($"   • Files per second: {result.FilesPerSecond:F1}");
            }
        }

        if (result.FailedFiles > 0)
        {
            message.AppendLine();
            message.AppendLine($"⚠️ Failed files: {result.FailedFiles:N0}");
        }

        if (result.HasErrors)
        {
            message.AppendLine();
            message.AppendLine("❌ Errors:");
            IEnumerable<string> errors = result.Errors.Take(3);
            foreach (string error in errors)
            {
                message.AppendLine($"   • {error}");
            }

            if (result.Errors.Count > 3)
            {
                message.AppendLine($"   • ... and {result.Errors.Count - 3} more error(s)");
            }
        }

        ShowMessage(message.ToString(), title, icon);
    }

    /// <summary>
    /// Displays a folder selection dialog allowing the user to choose a directory.
    /// </summary>
    /// <param name="description">The helpful description text displayed within the dialog.</param>
    /// <returns>The selected folder path if the user confirms; otherwise, null.</returns>
    public string? ShowFolderDialog(string description)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new() { Description = description };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    /// <summary>
    /// Displays a file open dialog allowing the user to select a single file.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string (e.g., "Text files (*.txt)|*.txt"). Defaults to all files.</param>
    /// <returns>The fully qualified path of the selected file; otherwise, null if canceled.</returns>
    public string? ShowOpenFileDialog(string title, string filter = "All files (*.*)|*.*")
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            Multiselect = false,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Displays a file open dialog allowing the user to select multiple files.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string (e.g., "Images (*.png;*.jpg)|*.png;*.jpg"). Defaults to all files.</param>
    /// <returns>An array of selected file paths if confirmed; otherwise, null.</returns>
    public string[]? ShowOpenMultipleFilesDialog(string title, string filter = "All files (*.*)|*.*")
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            Multiselect = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    /// <summary>
    /// Displays a save file dialog allowing the user to specify a target file path.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string controlling selectable file extensions. Defaults to all files.</param>
    /// <param name="defaultFileName">An optional default file name pre-populated in the dialog.</param>
    /// <returns>The chosen file path if confirmed; otherwise, null.</returns>
    public string? ShowSaveFileDialog(string title, string filter = "All files (*.*)|*.*", string defaultFileName = "")
    {
        Microsoft.Win32.SaveFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Formats a raw byte count into a human-readable string using binary multiples (KB, MB, GB, TB).
    /// </summary>
    /// <param name="bytes">The number of bytes to format.</param>
    /// <returns>A formatted size string (e.g., "1.5 MB"). Returns "0 B" for zero.</returns>
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0)
        {
            return "0 B";
        }

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        double size = Math.Abs(bytes);
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> into a concise human-readable representation.
    /// </summary>
    /// <param name="duration">The elapsed time to format.</param>
    /// <returns>
    /// A friendly textual representation (e.g., "&lt; 1 second", "5.2 seconds", "MM:SS", or "H:MM:SS")
    /// depending on the magnitude of the duration.
    /// </returns>
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 1 ? "< 1 second"
            : duration.TotalMinutes < 1 ? $"{duration.TotalSeconds:F1} seconds"
            : duration.TotalHours < 1 ? $"{duration.Minutes}:{duration.Seconds:D2}"
            : $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
}
