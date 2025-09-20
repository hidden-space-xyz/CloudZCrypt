using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Factories.Interfaces;
using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace CloudZCrypt.Infrastructure.Services.Encryption.Algorithms;

public class CamelliaEncryptionService(IKeyDerivationServiceFactory keyDerivationServiceFactory) : BaseEncryptionService(keyDerivationServiceFactory), IEncryptionAlgorithmStrategy
{
    public EncryptionAlgorithm Id => EncryptionAlgorithm.Camellia;
    public string DisplayName => "Camellia-256 GCM";
    public string Description => "A 128‑bit block cipher with a 256‑bit key, jointly designed by NTT and Mitsubishi; performance and security margin comparable to AES. Supported in many international standards (RFCs, ISO/IEC) and suitable where non‑U.S.-origin algorithms or broader jurisdictional acceptance is desired. Used with GCM for AEAD.";
    public string Summary => "Best for international compliance";

    protected override async Task EncryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(true, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }

    protected override async Task DecryptStreamAsync(FileStream sourceStream, FileStream destinationStream, byte[] key, byte[] nonce)
    {
        CamelliaEngine camelliaEngine = new();
        GcmBlockCipher gcmCipher = new(camelliaEngine);
        AeadParameters parameters = new(new KeyParameter(key), MacSize, nonce);
        gcmCipher.Init(false, parameters);

        await ProcessFileWithCipherAsync(sourceStream, destinationStream, gcmCipher);
    }
}
