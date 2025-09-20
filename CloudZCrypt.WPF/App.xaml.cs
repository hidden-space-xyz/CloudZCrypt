using CloudZCrypt.Composition;
using CloudZCrypt.WPF.Services;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CloudZCrypt.WPF;
public partial class App : System.Windows.Application
{
    private readonly IServiceProvider serviceProvider;
    private MainWindowViewModel? mainViewModel;

    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        mainViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        serviceProvider.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (mainViewModel != null)
            mainViewModel.Dispose();

        if (serviceProvider is IDisposable disposable)
            disposable.Dispose();

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddDomainServices();
        services.AddApplicationServices();
    }
}
