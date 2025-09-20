namespace CloudZCrypt.Domain.Services.Interfaces;

public interface IKeyDerivationService
{
    byte[] DeriveKey(string password, byte[] salt, int keySize);
}
