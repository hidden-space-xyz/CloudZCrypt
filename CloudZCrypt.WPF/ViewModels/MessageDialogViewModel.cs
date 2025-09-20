using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace CloudZCrypt.WPF.ViewModels;

public class MessageDialogViewModel : INotifyPropertyChanged
{
    public string Message { get; }
    public string Title { get; }
    public string IconText { get; }
    public MediaBrush IconBrush { get; }
    public Visibility IconVisibility { get; }

    public MessageDialogViewModel(string message, string title, MessageBoxImage icon)
    {
        Message = message;
        Title = title;

        (IconText, IconBrush, IconVisibility) = GetIconProperties(icon);
    }

    private static (string iconText, MediaBrush iconBrush, Visibility visibility) GetIconProperties(MessageBoxImage icon)
    {
        return icon switch
        {
            MessageBoxImage.Information => ("\uE946", new SolidColorBrush(MediaColor.FromRgb(0, 120, 215)), Visibility.Visible),
            MessageBoxImage.Warning => ("\uE7BA", new SolidColorBrush(MediaColor.FromRgb(255, 185, 0)), Visibility.Visible),
            MessageBoxImage.Error => ("\uE783", new SolidColorBrush(MediaColor.FromRgb(232, 17, 35)), Visibility.Visible),
            MessageBoxImage.Question => ("\uE9CE", new SolidColorBrush(MediaColor.FromRgb(0, 120, 215)), Visibility.Visible),
            _ => (string.Empty, MediaBrushes.Transparent, Visibility.Collapsed)
        };
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}
