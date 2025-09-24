using CloudZCrypt.WPF.ViewModels;
using System.Windows;

namespace CloudZCrypt.WPF.Views;

/// <summary>
/// Represents a modal confirmation dialog window that displays a message and provides the user
/// with Yes and No response options. The dialog returns a <see cref="bool"/> result indicating
/// whether the user confirmed the action.
/// </summary>
/// <remarks>
/// This dialog is intended for simple, synchronous confirmation scenarios where a binary (Yes/No)
/// decision is required from the user. The dialog sets focus to the Yes button when loaded to
/// support keyboard-first interaction.
/// </remarks>
public partial class ConfirmationDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationDialog"/> class with the specified message,
    /// title, and optional owner window.
    /// </summary>
    /// <param name="message">The confirmation message to display to the user. Should be concise and action-oriented.</param>
    /// <param name="title">The title text displayed in the window's title bar.</param>
    /// <param name="owner">An optional owner <see cref="Window"/> that will own this dialog. May be null.</param>
    public ConfirmationDialog(string message, string title, Window? owner = null)
    {
        InitializeComponent();

        DataContext = new MessageDialogViewModel(message, title, MessageBoxImage.Question);

        if (owner != null)
        {
            Owner = owner;
        }

        Loaded += (s, e) => YesButton.Focus();
    }

    /// <summary>
    /// Handles the click event of the Yes button, sets the dialog result to true, and closes the window.
    /// </summary>
    /// <param name="sender">The source of the event (the Yes button).</param>
    /// <param name="e">The event data associated with the click action.</param>
    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the click event of the No button, sets the dialog result to false, and closes the window.
    /// </summary>
    /// <param name="sender">The source of the event (the No button).</param>
    /// <param name="e">The event data associated with the click action.</param>
    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
