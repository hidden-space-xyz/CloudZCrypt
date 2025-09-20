using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.Password;

namespace CloudZCrypt.Domain.Services.Interfaces
{
    public interface IPasswordService
    {
        PasswordStrengthAnalysis AnalyzePasswordStrength(string password);
        string GeneratePassword(int length, PasswordGenerationOptions options);
    }
}
