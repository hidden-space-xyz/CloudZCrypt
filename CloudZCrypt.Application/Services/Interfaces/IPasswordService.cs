using CloudZCrypt.Application.DataTransferObjects.Passwords;

namespace CloudZCrypt.Application.Services.Interfaces
{
    public interface IPasswordService
    {
        PasswordStrengthResult EvaluatePasswordStrength(string password);
    }
}