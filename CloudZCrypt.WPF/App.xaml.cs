using CloudZCrypt.Composition;
using CloudZCrypt.WPF.Services;
using CloudZCrypt.WPF.Services.Interfaces;
using CloudZCrypt.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CloudZCrypt.WPF;

/// <summary>
/// Represents the WPF application entry point, configuring dependency injection services
/// and orchestrating startup and shutdown lifecycle events.
/// </summary>
/// <remarks>
/// This class bootstraps the application by registering UI, domain, and application services
/// using <see cref="IServiceCollection"/>, then resolves and displays the <see cref="MainWindow"/>.
/// It also ensures that disposable resources, including the root <see cref="IServiceProvider"/>,
/// are properly released on application exit.
/// </remarks>
public partial class App : System.Windows.Application
{
    private readonly IServiceProvider serviceProvider;
    private MainWindowViewModel? mainViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class and configures the
    /// dependency injection container used throughout the application.
    /// </summary>
    public App()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Handles application startup logic, resolving the <see cref="MainWindowViewModel"/>
    /// and displaying the main window to the user.
    /// </summary>
    /// <param name="e">The startup event arguments supplied by the WPF framework.</param>
    /// <exception cref="InvalidOperationException">Thrown if required services such as <see cref="MainWindowViewModel"/> or <see cref="MainWindow"/> are not registered.</exception>
    protected override void OnStartup(StartupEventArgs e)
    {
        mainViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        serviceProvider.GetRequiredService<MainWindow>().Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Handles application shutdown logic, disposing managed resources such as the
    /// resolved <see cref="MainWindowViewModel"/> and the root service provider.
    /// </summary>
    /// <param name="e">The exit event arguments supplied by the WPF framework.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        if (mainViewModel != null)
        {
            mainViewModel.Dispose();
        }

        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Registers UI, domain, and application layer services with the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to populate with application service registrations. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddDomainServices();
        services.AddApplicationServices();
    }
}
