using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
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

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        Assembly applicationAssembly = typeof(Result).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();

        return services;
    }
}
