using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects.Password;

/// <summary>
/// Domain value object representing password strength analysis result
/// </summary>
public sealed record PasswordStrengthAnalysis(
    PasswordStrength Strength,
    string Description,
    double Score);