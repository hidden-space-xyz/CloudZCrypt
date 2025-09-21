using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Infrastructure.Factories;
using CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;
using CloudZCrypt.Infrastructure.Services.FileSystem;
using CloudZCrypt.Infrastructure.Services.KeyDerivation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CloudZCrypt.Composition;

/// <summary>
/// Provides extension methods for registering CloudZCrypt domain and application level services
/// with an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// These methods centralize the dependency injection configuration for the solution, grouping
/// registrations by logical layer (Domain vs Application). Typical usage:
/// <code>
/// var services = new ServiceCollection();
/// services
///     .AddDomainServices()
///     .AddApplicationServices();
/// </code>
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registers domain layer services, factories, and algorithm strategies required by the encryption
    /// and key derivation subsystems.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to. Must not be null.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance to allow fluent chaining.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Factories
        services.AddSingleton<IKeyDerivationServiceFactory, KeyDerivationServiceFactory>();
        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();

        // Strategies
        services.AddSingleton<IKeyDerivationAlgorithmStrategy, Argon2IdKeyDerivationService>();
        services.AddSingleton<IKeyDerivationAlgorithmStrategy, Pbkdf2KeyDerivationService>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, AesEncryptionService>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, TwofishEncryptionService>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, SerpentEncryptionService>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, ChaCha20EncryptionService>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, CamelliaEncryptionService>();

        // Services
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IFileOperationsService, FileOperationsService>();
        services.AddSingleton<ISystemStorageService, SystemStorageService>();

        return services;
    }

    /// <summary>
    /// Registers application layer services including validators and orchestration components.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to. Must not be null.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance to allow fluent chaining.</returns>
    /// <remarks>
    /// All FluentValidation validators contained in the Application assembly are automatically discovered
    /// and registered through <see cref="ServiceCollectionExtensions.AddValidatorsFromAssembly(IServiceCollection, Assembly, ServiceLifetime, bool)"/>.
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(Result).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();

        return services;
    }
}
