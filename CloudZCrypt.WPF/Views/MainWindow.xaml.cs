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
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model that supplies state, commands, and interaction logic. Must not be null.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
