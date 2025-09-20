using CloudZCrypt.Application.Common.Behaviors;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
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

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IKeyDerivationServiceFactory, KeyDerivationServiceFactory>();
        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();

        // Keyed derivation services (used at runtime by factory)
        services.AddKeyedTransient<IKeyDerivationService, Argon2IdKeyDerivationService>(KeyDerivationAlgorithm.Argon2id);
        services.AddKeyedTransient<IKeyDerivationService, Pbkdf2KeyDerivationService>(KeyDerivationAlgorithm.PBKDF2);

        // Keyed encryption services (used at runtime by factory)
        services.AddKeyedTransient<IEncryptionService, AesEncryptionService>(EncryptionAlgorithm.Aes);
        services.AddKeyedTransient<IEncryptionService, TwofishEncryptionService>(EncryptionAlgorithm.Twofish);
        services.AddKeyedTransient<IEncryptionService, SerpentEncryptionService>(EncryptionAlgorithm.Serpent);
        services.AddKeyedTransient<IEncryptionService, ChaCha20EncryptionService>(EncryptionAlgorithm.ChaCha20);
        services.AddKeyedTransient<IEncryptionService, CamelliaEncryptionService>(EncryptionAlgorithm.Camellia);

        // Strategy collections for UI metadata binding
        services.AddTransient<IEncryptionAlgorithmStrategy, AesEncryptionService>();
        services.AddTransient<IEncryptionAlgorithmStrategy, TwofishEncryptionService>();
        services.AddTransient<IEncryptionAlgorithmStrategy, SerpentEncryptionService>();
        services.AddTransient<IEncryptionAlgorithmStrategy, ChaCha20EncryptionService>();
        services.AddTransient<IEncryptionAlgorithmStrategy, CamelliaEncryptionService>();

        services.AddTransient<IKeyDerivationAlgorithmStrategy, Argon2IdKeyDerivationService>();
        services.AddTransient<IKeyDerivationAlgorithmStrategy, Pbkdf2KeyDerivationService>();

        // Stateless domain and infrastructure services can be singletons
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddSingleton<IFileOperationsService, FileOperationsService>();
        services.AddSingleton<ISystemStorageService, SystemStorageService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(Result).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();

        return services;
    }
}
