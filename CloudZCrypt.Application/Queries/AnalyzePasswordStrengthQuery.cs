using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.DataTransferObjects.Passwords;

namespace CloudZCrypt.Application.Queries;

/// <summary>
/// Query to analyze password strength
/// </summary>
public record AnalyzePasswordStrengthQuery : IQuery<Result<PasswordStrengthResult>>
{
    public required string Password { get; init; }
}