using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;

namespace CloudZCrypt.Application.Queries;
public record GeneratePasswordQuery : IQuery<Result<string>>
{
    public int Length { get; init; } = 16;
    public bool IncludeUppercase { get; init; } = true;
    public bool IncludeLowercase { get; init; } = true;
    public bool IncludeNumbers { get; init; } = true;
    public bool IncludeSpecialCharacters { get; init; } = true;
    public bool ExcludeSimilarCharacters { get; init; } = false;
}
