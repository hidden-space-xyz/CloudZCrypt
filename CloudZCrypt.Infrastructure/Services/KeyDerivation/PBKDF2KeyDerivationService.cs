using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.KeyDerivation;

public class PBKDF2KeyDerivationService : IKeyDerivationService
{
    private const int Iterations = 100000; // 100k iterations - faster than Argon2id but still secure

    public byte[] DeriveKey(string password, byte[] salt, int keySize)
    {
        byte[]? passwordBytes = null;
        byte[]? key = null;

        try
        {
            // Convert password to UTF-8 bytes
            passwordBytes = Encoding.UTF8.GetBytes(password);

            // Create PBKDF2 generator with SHA-256
            Pkcs5S2ParametersGenerator pbkdf2 = new();
            pbkdf2.Init(passwordBytes, salt, Iterations);

            // Generate the key
            KeyParameter keyParam = (KeyParameter)pbkdf2.GenerateDerivedMacParameters(keySize);
            key = keyParam.GetKey();

            // Return a copy of the key
            byte[] result = new byte[key.Length];
            Array.Copy(key, result, key.Length);
            return result;
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Error deriving key with PBKDF2", ex);
        }
        finally
        {
            // Clean sensitive data from memory
            if (passwordBytes != null)
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            if (key != null)
                Array.Clear(key, 0, key.Length);
        }
    }
}