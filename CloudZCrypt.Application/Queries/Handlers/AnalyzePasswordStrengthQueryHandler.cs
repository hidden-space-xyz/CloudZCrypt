using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Domain.Services.Interfaces;

namespace CloudZCrypt.Application.Queries.Handlers;

/// <summary>
/// Handler for the AnalyzePasswordStrengthQuery
/// </summary>
public class AnalyzePasswordStrengthQueryHandler(IPasswordService passwordService) : IQueryHandler<AnalyzePasswordStrengthQuery, Result<PasswordStrengthResult>>
{
    public async Task<Result<PasswordStrengthResult>> Handle(AnalyzePasswordStrengthQuery request, CancellationToken cancellationToken)
    {
        try
        {
            PasswordStrengthResult result = await Task.Run(() => passwordService.AnalyzePasswordStrength(request.Password), cancellationToken);

            return Result<PasswordStrengthResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PasswordStrengthResult>.Failure($"Failed to analyze password strength: {ex.Message}");
        }
    }
}