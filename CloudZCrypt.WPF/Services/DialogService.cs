using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.Views;
using System.Text;
using System.Windows;

namespace CloudZCrypt.WPF.Services;

public class DialogService : IDialogService
{
    public bool ShowConfirmation(string message, string title)
    {
        Window? owner = System.Windows.Application.Current?.MainWindow;
        ConfirmationDialog dialog = new(message, title, owner);
        return dialog.ShowDialog() == true;
    }

    public void ShowMessage(string message, string title, MessageBoxImage icon)
    {
        Window? owner = System.Windows.Application.Current?.MainWindow;
        MessageDialog dialog = new(message, title, icon, owner);
        dialog.ShowDialog();
    }

    public void ShowProcessingWarning(string message, string title)
    {
        ShowMessage($"⚠️ {message}", title, MessageBoxImage.Warning);
    }

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

    public string? ShowFolderDialog(string description)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new() { Description = description };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

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
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 1 ? "< 1 second"
            : duration.TotalMinutes < 1 ? $"{duration.TotalSeconds:F1} seconds"
            : duration.TotalHours < 1 ? $"{duration.Minutes}:{duration.Seconds:D2}"
            : $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
}
