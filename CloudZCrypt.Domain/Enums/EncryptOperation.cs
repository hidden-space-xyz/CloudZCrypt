namespace CloudZCrypt.Domain.Enums;

/// <summary>
/// Specifies the cryptographic operation to be performed.
/// </summary>
/// <remarks>
/// Use this enumeration to clearly indicate whether an encryption or decryption workflow
/// should be invoked. It is typically supplied to services orchestrating file or data
/// protection logic to branch behavior accordingly.
/// </remarks>
public enum EncryptOperation
{
    /// <summary>
    /// Represents an encryption operation that converts plaintext or raw input data into its encrypted (ciphertext) form.
    /// </summary>
    Encrypt,

    /// <summary>
    /// Represents a decryption operation that converts previously encrypted data (ciphertext) back into its original plaintext form.
    /// </summary>
    Decrypt,
}
