using System.Windows;
using CloudZCrypt.WPF.ViewModels;

namespace CloudZCrypt.WPF;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
