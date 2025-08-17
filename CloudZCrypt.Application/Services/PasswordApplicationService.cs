using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Application.Queries;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;
using MediatR;

namespace CloudZCrypt.Application.Services;

internal class PasswordApplicationService(IMediator mediator) : IPasswordApplicationService
{
    public async Task<Result<string>> GeneratePasswordAsync(
        int length,
        PasswordGenerationOptions passwordCompositionOptions,
        CancellationToken cancellationToken = default)
    {
        GeneratePasswordQuery query = new()
        {
            Length = length,
            IncludeUppercase = passwordCompositionOptions.HasFlag(PasswordGenerationOptions.IncludeUppercase),
            IncludeLowercase = passwordCompositionOptions.HasFlag(PasswordGenerationOptions.IncludeLowercase),
            IncludeNumbers = passwordCompositionOptions.HasFlag(PasswordGenerationOptions.IncludeNumbers),
            IncludeSpecialCharacters = passwordCompositionOptions.HasFlag(PasswordGenerationOptions.IncludeSpecialCharacters),
            ExcludeSimilarCharacters = passwordCompositionOptions.HasFlag(PasswordGenerationOptions.ExcludeSimilarCharacters)
        };

        return await mediator.Send(query, cancellationToken);
    }
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
