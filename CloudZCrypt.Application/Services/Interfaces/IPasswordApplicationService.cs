using CloudZCrypt.Application.Common.Models;
using CloudZCrypt.Domain.DataTransferObjects.Passwords;

namespace CloudZCrypt.Application.Services.Interfaces
{
    public interface IPasswordApplicationService
    {
        Task<Result<PasswordStrengthResult>> AnalyzePasswordStrengthAsync(string password, CancellationToken cancellationToken = default);
        Task<Result<string>> GeneratePasswordAsync(int length, PasswordCompositionOptions passwordCompositionOptions, CancellationToken cancellationToken = default);
    }
}