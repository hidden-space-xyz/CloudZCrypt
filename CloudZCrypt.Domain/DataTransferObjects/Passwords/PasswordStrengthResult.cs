using CloudZCrypt.Domain.Constants;

namespace CloudZCrypt.Domain.DataTransferObjects.Passwords
{
    public class PasswordStrengthResult
    {
        public PasswordStrength Strength { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
