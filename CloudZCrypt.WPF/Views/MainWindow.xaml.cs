using System.Windows;
using CloudZCrypt.WPF.ViewModels;

namespace CloudZCrypt.WPF;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        this.Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            viewModel.Dispose();
        }
        catch
        {
            // Ignore cleanup errors during shutdown
        }
    }
}
