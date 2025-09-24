using CloudZCrypt.WPF.ViewModels;
using System.Windows;

namespace CloudZCrypt.WPF;

/// <summary>
/// Represents the main application window for the CloudZCrypt WPF client.
/// </summary>
/// <remarks>
/// This window is the primary visual host for the application and is bound to a <see cref="MainWindowViewModel"/>
/// instance that orchestrates UI state, commands, and user interactions (file selection, password handling,
/// and cryptographic operations). The view model is supplied through dependency injection to promote testability
/// and separation of concerns.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// The strongly-typed view model providing presentation logic and command bindings for the window.
    /// </summary>
    private readonly MainWindowViewModel viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model that supplies state, commands, and interaction logic. Must not be null.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;

        this.Closing += MainWindow_Closing;
    }

    /// <summary>
    /// Handles the window <see cref="Window.Closing"/> event to perform graceful disposal of the bound view model.
    /// </summary>
    /// <param name="sender">The event source (the window instance).</param>
    /// <param name="e">Event arguments containing cancellation information for the close operation.</param>
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
