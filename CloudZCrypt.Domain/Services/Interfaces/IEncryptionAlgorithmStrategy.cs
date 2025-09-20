using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Strategy interface that exposes metadata for an encryption algorithm.
/// Extends <see cref="IEncryptionService"/> so existing encryption logic is reused.
/// </summary>
public interface IEncryptionAlgorithmStrategy : IEncryptionService
{
    EncryptionAlgorithm Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Summary { get; }
}
