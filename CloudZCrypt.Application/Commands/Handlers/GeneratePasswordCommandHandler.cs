using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Application.Commands.Handlers;

/// <summary>
/// Handler for the GeneratePasswordQuery
/// </summary>
public class GeneratePasswordCommandHandler(IPasswordService passwordService) : ICommandHandler<GeneratePasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(GeneratePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            PasswordCompositionOptions options = PasswordCompositionOptions.None;

            if (request.IncludeUppercase)
                options |= PasswordCompositionOptions.IncludeUppercase;

            if (request.IncludeLowercase)
                options |= PasswordCompositionOptions.IncludeLowercase;

            if (request.IncludeNumbers)
                options |= PasswordCompositionOptions.IncludeNumbers;

            if (request.IncludeSpecialCharacters)
                options |= PasswordCompositionOptions.IncludeSpecialCharacters;

            if (request.ExcludeSimilarCharacters)
                options |= PasswordCompositionOptions.ExcludeSimilarCharacters;

            string password = await Task.Run(() => passwordService.GeneratePassword(request.Length, options), cancellationToken);

            return Result<string>.Success(password);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Failed to generate password: {ex.Message}");
        }
    }
}