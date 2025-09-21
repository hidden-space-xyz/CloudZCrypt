using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;
using System.Windows;

namespace CloudZCrypt.WPF.Services.Interfaces;

/// <summary>
/// Defines a contract for displaying user interface dialogs, message boxes, and file system selection prompts
/// within the WPF application.
/// </summary>
/// <remarks>
/// Implementations abstract the underlying UI framework (e.g., <see cref="System.Windows.MessageBox"/>) to
/// facilitate unit testing and separation of concerns. Typical consumers are view models that require
/// user feedback, confirmation, or presentation of processing results without directly invoking UI primitives.
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Displays an informational, warning, error, or other categorized message to the user.
    /// </summary>
    /// <param name="message">The textual content to display. Should not be null or empty.</param>
    /// <param name="title">The caption of the dialog window.</param>
    /// <param name="icon">The icon indicating the nature of the message (information, warning, error, etc.).</param>
    void ShowMessage(string message, string title, MessageBoxImage icon);

    /// <summary>
    /// Displays a summarized result of a batch encryption or decryption processing operation, including success state and any errors.
    /// </summary>
    /// <param name="result">The aggregate processing result containing metrics and error details.</param>
    /// <param name="operation">The cryptographic operation performed (encrypt or decrypt).</param>
    /// <param name="sourceType">A descriptive label of the processed source (e.g., "File", "Folder", or logical grouping).</param>
    void ShowProcessingResult(
        FileProcessingResult result,
        EncryptOperation operation,
        string sourceType
    );

    /// <summary>
    /// Displays one or more validation error messages to the user in a consolidated form.
    /// </summary>
    /// <param name="errors">A collection of validation error strings to present. Should not be null; an empty sequence indicates no actionable errors.</param>
    void ShowValidationErrors(IEnumerable<string> errors);

    /// <summary>
    /// Displays a non-blocking or dismissible warning related to a processing scenario that did not fully fail.
    /// </summary>
    /// <param name="message">The warning message text to show.</param>
    /// <param name="title">The dialog caption indicating context for the warning.</param>
    void ShowProcessingWarning(string message, string title);

    /// <summary>
    /// Prompts the user to confirm or cancel a potentially destructive or consequential action.
    /// </summary>
    /// <param name="message">The confirmation question or instruction.</param>
    /// <param name="title">The dialog caption providing context.</param>
    /// <returns><c>true</c> if the user confirms the action; otherwise, <c>false</c>.</returns>
    bool ShowConfirmation(string message, string title);

    /// <summary>
    /// Opens a folder selection dialog allowing the user to choose a directory.
    /// </summary>
    /// <param name="description">Optional descriptive guidance displayed within the dialog to assist the user.</param>
    /// <returns>The full path of the selected folder, or <c>null</c> if the operation is canceled.</returns>
    string? ShowFolderDialog(string description);

    /// <summary>
    /// Opens a file selection dialog allowing the user to choose a single file.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string (e.g., "Text Files (*.txt)|*.txt"). Defaults to all files.</param>
    /// <returns>The full path of the selected file, or <c>null</c> if the user cancels.</returns>
    string? ShowOpenFileDialog(string title, string filter = "All files (*.*)|*.*");

    /// <summary>
    /// Opens a file selection dialog allowing the user to select multiple files simultaneously.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string (e.g., "Images (*.png;*.jpg)|*.png;*.jpg"). Defaults to all files.</param>
    /// <returns>An array of full file paths selected by the user, or <c>null</c> if the dialog is canceled.</returns>
    string[]? ShowOpenMultipleFilesDialog(string title, string filter = "All files (*.*)|*.*");

    /// <summary>
    /// Opens a save file dialog allowing the user to specify a target file path (and optionally name) for output.
    /// </summary>
    /// <param name="title">The dialog window title.</param>
    /// <param name="filter">The file type filter string controlling selectable file extensions. Defaults to all files.</param>
    /// <param name="defaultFileName">An optional suggested file name pre-populated in the dialog input field.</param>
    /// <returns>The full path chosen by the user for saving, or <c>null</c> if the dialog is canceled.</returns>
    string? ShowSaveFileDialog(
        string title,
        string filter = "All files (*.*)|*.*",
        string defaultFileName = ""
    );
}
