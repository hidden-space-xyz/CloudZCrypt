using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class TwofishEncryptionService : BaseEncryptionService
{
    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        // Configure BouncyCastle Twofish-GCM engine
        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        // Configure BouncyCastle Twofish-GCM engine
        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}