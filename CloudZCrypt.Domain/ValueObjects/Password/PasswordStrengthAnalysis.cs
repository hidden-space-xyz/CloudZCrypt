using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects.Password;

public sealed record PasswordStrengthAnalysis(
    PasswordStrength Strength,
    string Description,
    double Score
);
