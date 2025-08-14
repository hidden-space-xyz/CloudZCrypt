using CloudZCrypt.Composition;
using CloudZCrypt.WPF.Services;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CloudZCrypt.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _serviceProvider.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Views
        services.AddSingleton<MainWindow>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Services
        services.AddSingleton<IDialogService, DialogService>();

        // Application services
        services.AddEncryptionServices();
        services.AddUseCases();
    }
}
