using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Strategies.Obfuscation;

internal class NoObfuscationStrategy : INameObfuscationStrategy
{
    public NameObfuscationMode Id => NameObfuscationMode.None;

    public string DisplayName => "None";

    public string Description =>
        "Preserves the original filename unchanged. Use when filename privacy is not required "
        + "and human-readable organization should be retained.";

    public string Summary => "Best if filename obfuscation is not needed";

    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        return originalFileName;
    }
}
