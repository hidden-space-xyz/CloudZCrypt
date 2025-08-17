using CloudZCrypt.Composition;
using CloudZCrypt.WPF.Services;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CloudZCrypt.WPF;
public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;
    private MainWindowViewModel? _mainViewModel;

    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Register cleanup events
        RegisterCleanupEvents();

        // Register exception handling
        RegisterExceptionHandling();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddDomainServices();
        services.AddApplicationServices();
    }

    private void RegisterCleanupEvents()
    {
        this.Exit += OnApplicationExit;
        this.SessionEnding += OnApplicationExit;
        AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;
    }

    private void RegisterExceptionHandling()
    {
        this.DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private async void OnApplicationExit(object? sender, EventArgs e)
    {
        await PerformCleanupAsync();
        _serviceProvider?.Dispose();
    }

    private async void OnUnhandledException(object? sender, EventArgs e)
    {
        await PerformCleanupAsync();

        // Let the application handle unhandled exceptions normally
        if (e is System.Windows.Threading.DispatcherUnhandledExceptionEventArgs dispatcherArgs)
            dispatcherArgs.Handled = false;
    }

    private async Task PerformCleanupAsync()
    {
        try
        {
            if (_mainViewModel != null)
            {
                await _mainViewModel.EmergencyCleanupAsync();
                _mainViewModel.Dispose();
            }
        }
        catch
        {
            // Ignore cleanup errors during shutdown
        }
    }
}
