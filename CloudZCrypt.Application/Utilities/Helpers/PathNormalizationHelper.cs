namespace CloudZCrypt.Application.Utilities.Helpers;

internal static class PathNormalizationHelper
{
    internal static string? TryNormalize(string rawPath, out string? error)
    {
        error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            string expanded = Environment.ExpandEnvironmentVariables(rawPath.Trim());
            return Path.GetFullPath(expanded);
        }
        catch (Exception ex)
        {
            error = $"Invalid path: {ex.Message}";
            return null;
        }
    }
}
