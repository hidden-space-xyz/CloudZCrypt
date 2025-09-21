using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.DataTransferObjects.Passwords;

public record PasswordStrengthResult(PasswordStrength Strength, string Description, double Score);
