using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.Obfuscation;

/// <summary>
/// Provides a name obfuscation strategy that does not modify the original filename.
/// </summary>
/// <remarks>
/// This strategy is used when users want to maintain the original filenames during encryption,
/// preserving readability and file organization structure.
/// </remarks>
internal class NoObfuscationStrategy : INameObfuscationStrategy
{
    /// <summary>
    /// Gets the unique identifier representing no obfuscation.
    /// </summary>
    public NameObfuscationMode Id => NameObfuscationMode.None;

    /// <summary>
    /// Gets the human-readable display name for this strategy.
    /// </summary>
    public string DisplayName => "None";

    /// <summary>
    /// Gets a detailed description of this obfuscation strategy.
    /// </summary>
    public string Description =>
        "Preserves the original filename unchanged. Use when filename privacy is not required " +
        "and human-readable organization should be retained.";

    /// <summary>
    /// Gets a concise summary describing when this strategy is appropriate.
    /// </summary>
    public string Summary => "Best if filename obfuscation is not needed";

    /// <summary>
    /// Returns the original filename unchanged.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file (not used in this strategy).</param>
    /// <param name="originalFileName">The original filename to preserve.</param>
    /// <returns>The original filename without any modifications.</returns>
    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        return originalFileName;
    }
}