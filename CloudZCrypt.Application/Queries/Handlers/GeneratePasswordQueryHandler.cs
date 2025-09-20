using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using MediatR;

namespace CloudZCrypt.Application.Queries.Handlers;

public class GeneratePasswordQueryHandler(IPasswordService passwordService) : IRequestHandler<GeneratePasswordQuery, Result<string>>
{
    public Task<Result<string>> Handle(GeneratePasswordQuery request, CancellationToken cancellationToken)
    {
        PasswordGenerationOptions options = BuildPasswordOptions(request);
        string password = passwordService.GeneratePassword(request.Length, options);
        return Task.FromResult(Result<string>.Success(password));
    }

    private static PasswordGenerationOptions BuildPasswordOptions(GeneratePasswordQuery request)
    {
        PasswordGenerationOptions options = PasswordGenerationOptions.None;

        if (request.IncludeUppercase)
            options |= PasswordGenerationOptions.IncludeUppercase;

        if (request.IncludeLowercase)
            options |= PasswordGenerationOptions.IncludeLowercase;

        if (request.IncludeNumbers)
            options |= PasswordGenerationOptions.IncludeNumbers;

        if (request.IncludeSpecialCharacters)
            options |= PasswordGenerationOptions.IncludeSpecialCharacters;

        if (request.ExcludeSimilarCharacters)
            options |= PasswordGenerationOptions.ExcludeSimilarCharacters;

        return options;
    }
}
