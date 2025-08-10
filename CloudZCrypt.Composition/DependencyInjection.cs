using CloudZCrypt.Application.UseCases;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;
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


        return services;
    }

    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<EncryptFileUseCase>();

        return services;
    }
}
