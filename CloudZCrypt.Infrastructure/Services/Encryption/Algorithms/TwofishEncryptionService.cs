using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class TwofishEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : BaseEncryptionService(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Twofish;
    public string DisplayName => "Twofish-256 GCM";
    public string Description => "A flexible 128‑bit block cipher (up to 256‑bit keys), also an AES finalist. Offers solid cryptanalytic resilience with a different design philosophy (Feistel + key-dependent S‑boxes) for algorithmic diversity. Less commonly hardware-accelerated or standardized for AEAD modes.";
    public string Summary => "Best for design diversity";

    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {

        TwofishEngine twofishEngine = new();
        GcmBlockCipher gcmCipher = new(twofishEngine);
        AeadParameters parameters = new(new KeyParameter(key), TagSize * 8, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
