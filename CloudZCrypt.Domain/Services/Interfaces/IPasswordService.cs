using CloudZCrypt.Domain.DataTransferObjects.Passwords;

namespace CloudZCrypt.Domain.Services.Interfaces
{
    public interface IPasswordService
    {
        PasswordStrengthResult AnalyzePasswordStrength(string password);
        string GeneratePassword(int length, PasswordCompositionOptions options);
    }
}
