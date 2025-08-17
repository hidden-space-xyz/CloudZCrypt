using CloudZCrypt.Application.Common.Behaviors;
using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
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
        services.AddSingleton<IKeyDerivationServiceFactory, KeyDerivationServiceFactory>();
        services.AddKeyedTransient<IKeyDerivationService, Argon2idKeyDerivationService>(KeyDerivationAlgorithm.Argon2id);
        services.AddKeyedTransient<IKeyDerivationService, PBKDF2KeyDerivationService>(KeyDerivationAlgorithm.PBKDF2);

        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();
        services.AddKeyedTransient<IEncryptionService, AesEncryptionService>(EncryptionAlgorithm.Aes);
        services.AddKeyedTransient<IEncryptionService, TwofishEncryptionService>(EncryptionAlgorithm.Twofish);
        services.AddKeyedTransient<IEncryptionService, SerpentEncryptionService>(EncryptionAlgorithm.Serpent);
        services.AddKeyedTransient<IEncryptionService, ChaCha20EncryptionService>(EncryptionAlgorithm.ChaCha20);
        services.AddKeyedTransient<IEncryptionService, CamelliaEncryptionService>(EncryptionAlgorithm.Camellia);

        services.AddScoped<IPasswordService, PasswordService>();

        // Virtual File System Services
        services.AddSingleton<IFileSystemService, FileSystemService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(Application.Common.Abstractions.ICommand).Assembly;

        // Add MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Add Application Services
        services.AddScoped<IFileEncryptionApplicationService, FileEncryptionApplicationService>();
        services.AddScoped<IPasswordApplicationService, PasswordApplicationService>();
        services.AddScoped<IFileSystemApplicationService, FileSystemApplicationService>();

        return services;
    }
}
