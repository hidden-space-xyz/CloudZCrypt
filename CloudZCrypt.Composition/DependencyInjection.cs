using CloudZCrypt.Application.UseCases;
using CloudZCrypt.Domain.Constants;
using CloudZCrypt.Domain.Factories;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;
using CloudZCrypt.Infrastructure.Services.KeyDerivation;
using Microsoft.Extensions.DependencyInjection;


namespace CloudZCrypt.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services)
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

        return services;
    }

    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<EncryptFileUseCase>();

        return services;
    }
}
