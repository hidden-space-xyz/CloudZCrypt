using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class SerpentEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : BaseEncryptionService(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Serpent;
    public string DisplayName => "Serpent-256 GCM";
    public string Description => "A conservative 128‑bit block cipher finalist from the AES competition, designed with a large security margin and a 256‑bit key option. Typically slower than AES and Camellia. When wrapped in GCM it provides AEAD, but performance costs make it niche for high-assurance or defense-in-depth scenarios.";
    public string Summary => "Best for high-security purposes (slow)";

    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        SerpentEngine serpentEngine = new();
        GcmBlockCipher gcmCipher = new(serpentEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        SerpentEngine serpentEngine = new();
        GcmBlockCipher gcmCipher = new(serpentEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
