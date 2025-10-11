namespace CloudZCrypt.Domain.Enums;

/// <summary>
/// Specifies filename obfuscation modes applied during encryption.
/// </summary>
public enum NameObfuscationMode
{
    /// <summary>
    /// Do not obfuscate. Keep original filename.
    /// </summary>
    None = 0,

    /// <summary>
    /// Replace the filename with a randomly generated GUID.
    /// </summary>
    Guid = 1,

    /// <summary>
    /// Replace the filename with the SHA-256 hex digest of the file contents.
    /// </summary>
    Sha256 = 2,

    /// <summary>
    /// Replace the filename with the SHA-512 hex digest of the file contents.
    /// </summary>
    Sha512 = 3,
}
