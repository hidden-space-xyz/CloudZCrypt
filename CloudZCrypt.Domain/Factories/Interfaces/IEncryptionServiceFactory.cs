using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Domain.Factories.Interfaces;

/// <summary>
/// Defines a factory responsible for creating concrete <see cref="IEncryptionAlgorithmStrategy"/> instances
/// based on a specified <see cref="EncryptionAlgorithm"/> value.
/// </summary>
/// <remarks>
/// This abstraction centralizes the logic for selecting and instantiating the appropriate encryption
/// strategy, helping clients remain agnostic of concrete implementation details. Implementations may
/// leverage dependency injection, internal registries, or reflection-based discovery to resolve the
/// correct strategy.
/// <para>
/// Example usage:
/// <code><![CDATA[
/// var strategy = encryptionServiceFactory.Create(EncryptionAlgorithm.Aes);
/// bool success = await strategy.EncryptFileAsync(sourcePath, destinationPath, password, KeyDerivationAlgorithm.Argon2id);
/// ]]></code>
/// </para>
/// </remarks>
public interface IEncryptionServiceFactory
{
    /// <summary>
    /// Creates an <see cref="IEncryptionAlgorithmStrategy"/> instance corresponding to the specified
    /// <paramref name="algorithm"/>.
    /// </summary>
    /// <param name="algorithm">The <see cref="EncryptionAlgorithm"/> identifying the desired encryption algorithm.</param>
    /// <returns>An initialized <see cref="IEncryptionAlgorithmStrategy"/> for the requested algorithm.</returns>
    /// <exception cref="System.NotSupportedException">Thrown if the specified <paramref name="algorithm"/> is not supported by the factory implementation.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the provided <paramref name="algorithm"/> value is outside the defined enumeration range.</exception>
    IEncryptionAlgorithmStrategy Create(EncryptionAlgorithm algorithm);
}
