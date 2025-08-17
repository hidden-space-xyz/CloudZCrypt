using CloudZCrypt.Domain.Factories.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class CamelliaEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : BaseEncryptionService(keyDerivationServiceFactory)
{
    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
