using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Application.DataTransferObjects.Passwords;
using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.Services.Interfaces
{
    public interface IPasswordApplicationService
    {
        Task<Result<PasswordStrengthResult>> AnalyzePasswordStrengthAsync(string password, CancellationToken cancellationToken = default);
        Task<Result<string>> GeneratePasswordAsync(int length, PasswordGenerationOptions passwordCompositionOptions, CancellationToken cancellationToken = default);
    }
}
