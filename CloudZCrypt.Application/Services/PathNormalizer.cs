using CloudZCrypt.Application.Services.Interfaces;

namespace CloudZCrypt.Application.Services;

internal sealed class PathNormalizer : IPathNormalizer
{
    public string? TryNormalize(string rawPath, out string? error)
    {
        error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }
            string expanded = Environment.ExpandEnvironmentVariables(rawPath.Trim());
            string full = Path.GetFullPath(expanded);
            return full;
        }
        catch (Exception ex)
        {
            error = $"Invalid path: {ex.Message}";
            return null;
        }
    }
}
