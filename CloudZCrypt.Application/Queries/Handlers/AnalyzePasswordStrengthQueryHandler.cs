using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.Password;
using MediatR;

namespace CloudZCrypt.Application.Queries.Handlers;

public class AnalyzePasswordStrengthQueryHandler(IPasswordService passwordService) : IRequestHandler<AnalyzePasswordStrengthQuery, Result<PasswordStrengthResult>>
{
    public Task<Result<PasswordStrengthResult>> Handle(AnalyzePasswordStrengthQuery request, CancellationToken cancellationToken)
    {
        PasswordStrengthAnalysis domainResult = passwordService.AnalyzePasswordStrength(request.Password);

        PasswordStrengthResult result = new(
            domainResult.Strength,
            domainResult.Description,
            domainResult.Score);

        return Task.FromResult(Result<PasswordStrengthResult>.Success(result));
    }
}
