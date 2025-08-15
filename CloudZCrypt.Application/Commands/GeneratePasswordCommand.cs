using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;

namespace CloudZCrypt.Application.Commands;

/// <summary>
/// Query to generate a password
/// </summary>
public record GeneratePasswordCommand : ICommand<Result<string>>
{
    public int Length { get; init; } = 16;
    public bool IncludeUppercase { get; init; } = true;
    public bool IncludeLowercase { get; init; } = true;
    public bool IncludeNumbers { get; init; } = true;
    public bool IncludeSpecialCharacters { get; init; } = true;
    public bool ExcludeSimilarCharacters { get; init; } = false;
}