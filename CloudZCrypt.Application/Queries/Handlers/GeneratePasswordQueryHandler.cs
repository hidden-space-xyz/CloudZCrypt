using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Application.Queries.Handlers;
public class GeneratePasswordQueryHandler(IPasswordService passwordService) : IQueryHandler<GeneratePasswordQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GeneratePasswordQuery request, CancellationToken cancellationToken)
    {
        try
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

            string password = await Task.Run(() => passwordService.GeneratePassword(request.Length, options), cancellationToken);

            return Result<string>.Success(password);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate password: {ex.Message}");
        }
    }
}
