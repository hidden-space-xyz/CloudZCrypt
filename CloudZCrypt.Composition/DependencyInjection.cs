using CloudZCrypt.Application.Common.Behaviors;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services;
using CloudZCrypt.Domain.Services.Interfaces;
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
        // Factory services
        services.AddSingleton<IKeyDerivationServiceFactory, KeyDerivationServiceFactory>();
        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();

        // Key derivation services
        services.AddKeyedTransient<IKeyDerivationService, Argon2idKeyDerivationService>(KeyDerivationAlgorithm.Argon2id);
        services.AddKeyedTransient<IKeyDerivationService, PBKDF2KeyDerivationService>(KeyDerivationAlgorithm.PBKDF2);

        // Encryption services
        services.AddKeyedTransient<IEncryptionService, AesEncryptionService>(EncryptionAlgorithm.Aes);
        services.AddKeyedTransient<IEncryptionService, TwofishEncryptionService>(EncryptionAlgorithm.Twofish);
        services.AddKeyedTransient<IEncryptionService, SerpentEncryptionService>(EncryptionAlgorithm.Serpent);
        services.AddKeyedTransient<IEncryptionService, ChaCha20EncryptionService>(EncryptionAlgorithm.ChaCha20);
        services.AddKeyedTransient<IEncryptionService, CamelliaEncryptionService>(EncryptionAlgorithm.Camellia);

        // Domain services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IFileProcessingDomainService, FileProcessingDomainService>();

        // Infrastructure services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddScoped<IFileOperationsService, FileOperationsService>();
        services.AddTransient<IOnDemandDecryptionService, OnDemandDecryptionService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(Application.Commands.EncryptFilesCommand).Assembly;

        // MediatR configuration
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Validators
        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
