using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects.FileSystem;
public sealed record VolumeConfiguration
{
    public string EncryptedDirectory { get; }
    public string TempDirectory { get; }
    public string Password { get; }
    public KeyDerivationAlgorithm KeyDerivationAlgorithm { get; }

    public VolumeConfiguration(
        string encryptedDirectory,
        string tempDirectory,
        string password,
        KeyDerivationAlgorithm keyDerivationAlgorithm)
    {
        if (string.IsNullOrWhiteSpace(encryptedDirectory))
            throw new ArgumentException("Encrypted directory cannot be null or empty", nameof(encryptedDirectory));

        if (string.IsNullOrWhiteSpace(tempDirectory))
            throw new ArgumentException("Temp directory cannot be null or empty", nameof(tempDirectory));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        EncryptedDirectory = encryptedDirectory;
        TempDirectory = tempDirectory;
        Password = password;
        KeyDerivationAlgorithm = keyDerivationAlgorithm;
    }
    public bool UsesSameLocation(VolumeConfiguration other)
        => string.Equals(EncryptedDirectory, other.EncryptedDirectory, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(TempDirectory, other.TempDirectory, StringComparison.OrdinalIgnoreCase);
}
