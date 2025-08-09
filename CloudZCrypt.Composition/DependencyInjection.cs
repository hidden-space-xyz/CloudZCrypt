using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using CloudZCrypt.Application.Interfaces.Files;
using CloudZCrypt.Application.UseCases;
using CloudZCrypt.Infrastructure.Encryption;
using CloudZCrypt.Infrastructure.Encryption.Algorithms;
using Microsoft.Extensions.DependencyInjection;


namespace CloudZCrypt.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services)
    {
        services.AddSingleton<IEncryptionServiceFactory, EncryptionServiceFactory>();
        services.AddKeyedTransient<IEncryptionService, AesEncryptionService>(EncryptionAlgorithm.Aes);
        services.AddKeyedTransient<IEncryptionService, TwofishEncryptionService>(EncryptionAlgorithm.Twofish);
        services.AddKeyedTransient<IEncryptionService, SerpentEncryptionService>(EncryptionAlgorithm.Serpent);
        services.AddKeyedTransient<IEncryptionService, ChaCha20EncryptionService>(EncryptionAlgorithm.ChaCha20);
        services.AddKeyedTransient<IEncryptionService, CamelliaEncryptionService>(EncryptionAlgorithm.Camellia);

        return services;
    }

    public static IServiceCollection AddStorageServices(this IServiceCollection services)
    {
        services.AddScoped<IFileProcessingService, FileProcessingService>();

        return services;
    }
}
