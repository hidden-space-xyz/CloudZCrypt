using System.Windows;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace CloudZCrypt.WPF.ViewModels;

/// <summary>
/// Represents the view model for a message dialog, encapsulating the message content, title,
/// and visual representation of an optional icon to be displayed in a WPF dialog window.
/// </summary>
/// <remarks>
/// This view model translates a <see cref="MessageBoxImage"/> enumeration value into a glyph,
/// brush, and visibility state suitable for binding in XAML. Glyphs are Unicode values that
/// assume an icon font such as Segoe Fluent Icons / Segoe MDL2 Assets is available.
/// Example usage:
/// <code>
/// var vm = new MessageDialogViewModel("Operation completed successfully.", "Information", MessageBoxImage.Information);
/// </code>
/// </remarks>
public class MessageDialogViewModel
{
    /// <summary>
    /// Gets the textual message to display to the user.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the title associated with the dialog.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the glyph (Unicode character) representing the icon corresponding to the specified <see cref="MessageBoxImage"/>.
    /// Returns an empty string when no icon should be displayed.
    /// </summary>
    public string IconText { get; }

    /// <summary>
    /// Gets the brush used to render the icon glyph when an icon is visible.
    /// </summary>
    public MediaBrush IconBrush { get; }

    /// <summary>
    /// Gets the visibility state indicating whether an icon should be displayed.
    /// </summary>
    public Visibility IconVisibility { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDialogViewModel"/> class with the specified
    /// message text, title, and icon type.
    /// </summary>
    /// <param name="message">The message text to present to the user. Can be multi-line.</param>
    /// <param name="title">The dialog title, typically shown in the window chrome.</param>
    /// <param name="icon">The <see cref="MessageBoxImage"/> value determining which icon (if any) is displayed.</param>
    public MessageDialogViewModel(string message, string title, MessageBoxImage icon)
    {
        Message = message;
        Title = title;

        (IconText, IconBrush, IconVisibility) = GetIconProperties(icon);
    }

    /// <summary>
    /// Maps a <see cref="MessageBoxImage"/> value to its corresponding glyph, brush, and visibility tuple.
    /// </summary>
    /// <param name="icon">The icon classification to translate.</param>
    /// <returns>
    /// A tuple containing: <c>iconText</c> (Unicode glyph), <c>iconBrush</c> (color brush), and <c>visibility</c>
    /// indicating whether an icon should be shown.
    /// </returns>
    private static (string iconText, MediaBrush iconBrush, Visibility visibility) GetIconProperties(
        MessageBoxImage icon
    )
    {
        return icon switch
        {
            MessageBoxImage.Information => (
                "\uE946",
                new SolidColorBrush(MediaColor.FromRgb(0, 120, 215)),
                Visibility.Visible
            ),
            MessageBoxImage.Warning => (
                "\uE7BA",
                new SolidColorBrush(MediaColor.FromRgb(255, 185, 0)),
                Visibility.Visible
            ),
            MessageBoxImage.Error => (
                "\uE783",
                new SolidColorBrush(MediaColor.FromRgb(232, 17, 35)),
                Visibility.Visible
            ),
            MessageBoxImage.Question => (
                "\uE9CE",
                new SolidColorBrush(MediaColor.FromRgb(0, 120, 215)),
                Visibility.Visible
            ),
            _ => (string.Empty, MediaBrushes.Transparent, Visibility.Collapsed),
        };
    }
}
