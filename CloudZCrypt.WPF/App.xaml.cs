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


        this.Exit += App_Exit;
        this.SessionEnding += App_SessionEnding;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;


        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
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

    #region Cleanup Event Handlers

    private async void App_Exit(object sender, ExitEventArgs e)
    {
        await PerformCleanup("Application Exit");
        _serviceProvider?.Dispose();
    }

    private async void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        await PerformCleanup("Session Ending");
        _serviceProvider?.Dispose();
    }

    private async void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        await PerformCleanup("Process Exit");
        _serviceProvider?.Dispose();
    }

    private async void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        await PerformCleanup($"Unhandled Exception: {e.Exception.Message}");


        e.Handled = false;
    }

    private async void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        Exception? exception = e.ExceptionObject as Exception;
        await PerformCleanup($"Unhandled Domain Exception: {exception?.Message ?? "Unknown"}");
    }

    private async Task PerformCleanup(string reason)
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

        }
    }

    #endregion
}
