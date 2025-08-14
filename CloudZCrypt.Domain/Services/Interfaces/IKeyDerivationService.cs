namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IKeyDerivationService
{
    /// <summary>
    /// Derives a cryptographic key from a password and salt
    /// </summary>
    /// <param name="password">The password to derive the key from</param>
    /// <param name="salt">The salt to use for key derivation</param>
    /// <param name="keySize">The desired key size in bits</param>
    /// <returns>The derived key as a byte array</returns>
    byte[] DeriveKey(string password, byte[] salt, int keySize);
}