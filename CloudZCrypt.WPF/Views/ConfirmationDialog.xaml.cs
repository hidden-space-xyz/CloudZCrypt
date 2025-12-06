using System.Windows;
using CloudZCrypt.WPF.ViewModels;

namespace CloudZCrypt.WPF.Views;

public partial class ConfirmationDialog : Window
{
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

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
