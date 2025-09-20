using CloudZCrypt.Application.Common.Models;
using MediatR;

namespace CloudZCrypt.Application.Queries;

// Refactored to primary-constructor record
public sealed record GeneratePasswordQuery(
    int Length = 16,
    bool IncludeUppercase = true,
    bool IncludeLowercase = true,
    bool IncludeNumbers = true,
    bool IncludeSpecialCharacters = true,
    bool ExcludeSimilarCharacters = false
) : IRequest<Result<string>>;
