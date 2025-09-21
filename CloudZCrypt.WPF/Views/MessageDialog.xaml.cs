using CloudZCrypt.WPF.ViewModels;
using System.Windows;

namespace CloudZCrypt.WPF.Views;

/// <summary>
/// Represents a simple modal dialog window used to display an informational message
/// to the user along with an optional icon and title.
/// </summary>
/// <remarks>
/// This dialog presents a single <c>OK</c> action to acknowledge the message. It can be
/// instantiated with an owner window to ensure it is centered and modal relative to
/// that window. Example usage:
/// <code>
/// new MessageDialog("Operation completed successfully.", "Success", MessageBoxImage.Information, this).ShowDialog();
/// </code>
/// </remarks>
public partial class MessageDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialog"/> class with the specified
    /// message content, window title, icon, and optional owner window.
    /// </summary>
    /// <param name="message">The textual content displayed to the user. Cannot be <c>null</c>; an empty string is allowed.</param>
    /// <param name="title">The caption displayed in the dialog's title bar.</param>
    /// <param name="icon">The icon to visually indicate the nature of the message (e.g., information, warning, error).</param>
    /// <param name="owner">An optional owner <see cref="Window"/> that this dialog will be modal to. May be <c>null</c>.</param>
    public MessageDialog(string message, string title, MessageBoxImage icon, Window? owner = null)
    {
        InitializeComponent();

        DataContext = new MessageDialogViewModel(message, title, icon);

        if (owner != null)
        {
            Owner = owner;
        }

        Loaded += (s, e) => OkButton.Focus();
    }

    /// <summary>
    /// Handles the click event of the OK button by setting the dialog result to <c>true</c>
    /// and closing the window.
    /// </summary>
    /// <param name="sender">The source of the event, typically the OK button.</param>
    /// <param name="e">The event data associated with the routed event.</param>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
