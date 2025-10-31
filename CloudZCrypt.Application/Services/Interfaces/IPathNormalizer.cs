namespace CloudZCrypt.Application.Services.Interfaces;

public interface IPathNormalizer
{
    string? TryNormalize(string rawPath, out string? error);
}
