using CloudZCrypt.Application.Commands;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Queries;
using CloudZCrypt.Application.Services.Interfaces;
using MediatR;

namespace CloudZCrypt.Application.Services;

internal class PasswordApplicationService(IMediator mediator) : IPasswordApplicationService
{
    /// <summary>
    /// Generates a password using CQRS query
    /// </summary>
    public async Task<Result<string>> GeneratePasswordAsync(
        int length,
        PasswordCompositionOptions passwordCompositionOptions,
        CancellationToken cancellationToken = default)
    {
        GeneratePasswordCommand query = new()
        {
            Length = length,
            IncludeUppercase = passwordCompositionOptions.HasFlag(PasswordCompositionOptions.IncludeUppercase),
            IncludeLowercase = passwordCompositionOptions.HasFlag(PasswordCompositionOptions.IncludeLowercase),
            IncludeNumbers = passwordCompositionOptions.HasFlag(PasswordCompositionOptions.IncludeNumbers),
            IncludeSpecialCharacters = passwordCompositionOptions.HasFlag(PasswordCompositionOptions.IncludeSpecialCharacters),
            ExcludeSimilarCharacters = passwordCompositionOptions.HasFlag(PasswordCompositionOptions.ExcludeSimilarCharacters)
        };

        return await mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Analyzes password strength using CQRS query
    /// </summary>
    public async Task<Result<PasswordStrengthResult>> AnalyzePasswordStrengthAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        AnalyzePasswordStrengthQuery query = new()
        {
            Password = password
        };

        return await mediator.Send(query, cancellationToken);
    }
}