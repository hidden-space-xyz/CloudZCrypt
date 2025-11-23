using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Strategies.Interfaces;

namespace CloudZCrypt.Infrastructure.Strategies.Obfuscation;

internal class GuidObfuscationStrategy : INameObfuscationStrategy
{
    public NameObfuscationMode Id => NameObfuscationMode.Guid;

    public string DisplayName => "GUID";

    public string Description =>
        "Replaces the filename with a randomly generated GUID. "
        + "Provides strong privacy by producing non-predictable, non-correlatable names for each file.";

    public string Summary => "Best for maximum privacy (random)";

    public string ObfuscateFileName(string sourceFilePath, string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string guidName = Guid.NewGuid().ToString();
        return guidName + extension;
    }
}
