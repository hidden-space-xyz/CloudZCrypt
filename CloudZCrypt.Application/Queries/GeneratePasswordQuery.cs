using CloudZCrypt.Application.Common.Models;
using MediatR;

namespace CloudZCrypt.Application.Queries;

public record GeneratePasswordQuery : IRequest<Result<string>>
{
    public int Length { get; init; } = 16;
    public bool IncludeUppercase { get; init; } = true;
    public bool IncludeLowercase { get; init; } = true;
    public bool IncludeNumbers { get; init; } = true;
    public bool IncludeSpecialCharacters { get; init; } = true;
    public bool ExcludeSimilarCharacters { get; init; } = false;
}
