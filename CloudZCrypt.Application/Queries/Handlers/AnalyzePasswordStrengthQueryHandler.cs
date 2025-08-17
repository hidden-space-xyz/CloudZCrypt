using CloudZCrypt.Application.Common.Abstractions;
using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.Password;

namespace CloudZCrypt.Application.Queries.Handlers;
public class AnalyzePasswordStrengthQueryHandler(IPasswordService passwordService) : IQueryHandler<AnalyzePasswordStrengthQuery, Result<PasswordStrengthResult>>
{
    public async Task<Result<PasswordStrengthResult>> Handle(AnalyzePasswordStrengthQuery request, CancellationToken cancellationToken)
    {
        try
        {
            PasswordStrengthAnalysis domainResult = await Task.Run(() => passwordService.AnalyzePasswordStrength(request.Password), cancellationToken);


            PasswordStrengthResult result = new(
                domainResult.Strength,
                domainResult.Description,
                domainResult.Score);

            return Result<PasswordStrengthResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PasswordStrengthResult>.Failure($"Failed to analyze password strength: {ex.Message}");
        }
    }
}
