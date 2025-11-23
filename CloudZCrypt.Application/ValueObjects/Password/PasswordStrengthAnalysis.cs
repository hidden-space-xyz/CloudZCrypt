using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.ValueObjects.Password;

public sealed record PasswordStrengthAnalysis(
    PasswordStrength Strength,
    string Description,
    double Score
);
