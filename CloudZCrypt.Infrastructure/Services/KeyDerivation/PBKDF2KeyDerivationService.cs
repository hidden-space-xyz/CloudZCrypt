using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace CloudZCrypt.Infrastructure.Services.KeyDerivation;

public class PBKDF2KeyDerivationService : IKeyDerivationService
{
    private const int Iterations = 100000;

    public byte[] DeriveKey(string password, byte[] salt, int keySize)
    {
        byte[]? passwordBytes = null;
        byte[]? key = null;

        try
        {

            passwordBytes = Encoding.UTF8.GetBytes(password);


            Pkcs5S2ParametersGenerator pbkdf2 = new();
            pbkdf2.Init(passwordBytes, salt, Iterations);


            KeyParameter keyParam = (KeyParameter)pbkdf2.GenerateDerivedMacParameters(keySize);
            key = keyParam.GetKey();


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

            if (passwordBytes != null)
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            if (key != null)
                Array.Clear(key, 0, key.Length);
        }
    }
}
