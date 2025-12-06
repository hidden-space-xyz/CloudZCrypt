using CloudZCrypt.WPF.ViewModels;
using System.Windows;

namespace CloudZCrypt.WPF.Views;

public partial class MessageDialog : Window
{
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

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
