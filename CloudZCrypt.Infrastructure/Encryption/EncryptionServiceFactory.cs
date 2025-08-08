using CloudZCrypt.Application.Constants;
using CloudZCrypt.Application.Interfaces.Encryption;
using CloudZCrypt.Infrastructure.Encryption.Algorithms;

namespace CloudZCrypt.Infrastructure.Encryption
{
    internal class EncryptionServiceFactory : IEncryptionServiceFactory
    {
        public IEncryptionService Create(EncryptionAlgorithm algorithm)
        {
            return algorithm switch
            {
                EncryptionAlgorithm.Aes => new AesEncryptionService(),
                EncryptionAlgorithm.Twofish => new TwofishEncryptionService(),
                EncryptionAlgorithm.Serpent => new SerpentEncryptionService(),
                EncryptionAlgorithm.ChaCha20 => new ChaCha20EncryptionService(),
                EncryptionAlgorithm.Camellia => new CamelliaEncryptionService(),
                _ => throw new NotSupportedException($"The encryption algorithm '{algorithm}' is not supported.")
            };
        }
    }
}
