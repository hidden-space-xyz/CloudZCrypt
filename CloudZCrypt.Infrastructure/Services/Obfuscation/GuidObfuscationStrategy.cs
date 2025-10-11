using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Services.Obfuscation;

/// <summary>
/// Provides a name obfuscation strategy that replaces filenames with randomly generated GUIDs.
/// </summary>
/// <remarks>
/// This strategy provides excellent privacy by generating completely random filenames that cannot
/// be correlated with the original content. Each filename is unique and unpredictable.
/// </remarks>
internal class GuidObfuscationStrategy : INameObfuscationStrategy
{
    /// <summary>
    /// Gets the unique identifier representing GUID obfuscation.
    /// </summary>
    public NameObfuscationMode Id => NameObfuscationMode.Guid;

    /// <summary>
    /// Gets the human-readable display name for this strategy.
    /// </summary>
    public string DisplayName => "GUID";

    /// <summary>
    /// Gets a detailed description of this obfuscation strategy.
    /// </summary>
    public string Description =>
        "Replaces the filename with a randomly generated GUID. " +
        "Provides strong privacy by producing non-predictable, non-correlatable names for each file.";

    /// <summary>
    /// Gets a concise summary describing when this strategy is appropriate.
    /// </summary>
    public string Summary => "Best for maximum privacy (random)";

    /// <summary>
    /// Generates a new GUID-based filename while preserving the original file extension.
    /// </summary>
    /// <param name="sourceFilePath">The path to the source file (not used in this strategy).</param>
    /// <param name="originalFileName">The original filename to obfuscate.</param>
    /// <returns>A GUID-based filename with the original file extension.</returns>
    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string guidName = Guid.NewGuid().ToString();
        return guidName + extension;
    }
}