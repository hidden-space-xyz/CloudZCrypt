using CloudZCrypt.Domain.Services.Interfaces;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;

namespace CloudZCrypt.Infrastructure.Services.KeyDerivation;

public class Argon2idKeyDerivationService : IKeyDerivationService
{
    private const int MemoryCost = 128 * 1024;
    private const int Iterations = 5;
    private const int Parallelism = 4;

    public byte[] DeriveKey(string password, byte[] salt, int keySize)
    {

        Argon2BytesGenerator argon2 = new();
        argon2.Init(new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithSalt(salt)
            .WithMemoryAsKB(MemoryCost)
            .WithIterations(Iterations)
            .WithParallelism(Parallelism)
            .Build());


        byte[] key = new byte[keySize / 8];
        char[]? passwordChars = null;

        try
        {

            passwordChars = password.ToCharArray();


            argon2.GenerateBytes(passwordChars, key);

            return key;
        }
        catch (Exception ex)
        {

            if (key != null)
                Array.Clear(key, 0, key.Length);

            throw new CryptographicException("Error deriving key with Argon2id", ex);
        }
        finally
        {

            if (passwordChars != null)
                Array.Clear(passwordChars, 0, passwordChars.Length);
        }
    }
}
