using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;

namespace CloudZCrypt.Application.Queries;
public record AnalyzePasswordStrengthQuery : IQuery<Result<PasswordStrengthResult>>
{
    public required string Password { get; init; }
}
