using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.Services.Interfaces;

/// <summary>
/// Strategy interface that exposes metadata for a key derivation algorithm.
/// Extends <see cref="IKeyDerivationService"/> so existing derivation logic is reused.
/// </summary>
public interface IKeyDerivationAlgorithmStrategy : IKeyDerivationService
{
    KeyDerivationAlgorithm Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Summary { get; }
}
