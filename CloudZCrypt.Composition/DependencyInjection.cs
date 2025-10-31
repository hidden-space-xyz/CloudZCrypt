using CloudZCrypt.Application.Services;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.Strategies.Interfaces;
using CloudZCrypt.Infrastructure.Factories;
using CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;
using CloudZCrypt.Infrastructure.Services.FileSystem;
using CloudZCrypt.Infrastructure.Services.KeyDerivation;
using CloudZCrypt.Infrastructure.Services.Obfuscation;
using Microsoft.Extensions.DependencyInjection;

namespace CloudZCrypt.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Factories
        services.AddSingleton<IKeyDerivationServiceFactory, KeyDerivationServiceFactory>();
        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();
        services.AddSingleton<INameObfuscationServiceFactory, NameObfuscationServiceFactory>();

        // Key Derivation Strategies
        services.AddSingleton<IKeyDerivationAlgorithmStrategy, Argon2IdKeyDerivationStrategy>();
        services.AddSingleton<IKeyDerivationAlgorithmStrategy, Pbkdf2KeyDerivationStrategy>();

        // Encryption Strategies
        services.AddSingleton<IEncryptionAlgorithmStrategy, AesEncryptionStrategy>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, TwofishEncryptionStrategy>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, SerpentEncryptionStrategy>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, ChaCha20EncryptionStrategy>();
        services.AddSingleton<IEncryptionAlgorithmStrategy, CamelliaEncryptionStrategy>();

        // Name Obfuscation Strategies
        services.AddSingleton<INameObfuscationStrategy, NoObfuscationStrategy>();
        services.AddSingleton<INameObfuscationStrategy, GuidObfuscationStrategy>();
        services.AddSingleton<INameObfuscationStrategy, Sha256ObfuscationStrategy>();
        services.AddSingleton<INameObfuscationStrategy, Sha512ObfuscationStrategy>();

        // Services
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IFileOperationsService, FileOperationsService>();
        services.AddSingleton<ISystemStorageService, SystemStorageService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();
        services.AddSingleton<IFileProcessingRequestValidator, FileProcessingRequestValidator>();
        services.AddSingleton<IFileProcessingWarningAnalyzer, FileProcessingWarningAnalyzer>();
        services.AddSingleton<IPathNormalizer, PathNormalizer>();
        services.AddSingleton<IManifestService, ManifestService>();

        return services;
    }
}
