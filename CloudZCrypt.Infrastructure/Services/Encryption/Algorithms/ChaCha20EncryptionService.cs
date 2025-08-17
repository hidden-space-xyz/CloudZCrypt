using CloudZCrypt.Domain.Factories.Interfaces;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class ChaCha20EncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : BaseEncryptionService(keyDerivationServiceFactory)
{
    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        ChaCha20Poly1305 chacha20Poly1305 = new();
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        chacha20Poly1305.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, chacha20Poly1305);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        ChaCha20Poly1305 chacha20Poly1305 = new();
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        chacha20Poly1305.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, chacha20Poly1305);
    }
}
