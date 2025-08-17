using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using MediatR;

namespace CloudZCrypt.Application.Queries;

public record AnalyzePasswordStrengthQuery : IRequest<Result<PasswordStrengthResult>>
{
    public required string Password { get; init; }
}
