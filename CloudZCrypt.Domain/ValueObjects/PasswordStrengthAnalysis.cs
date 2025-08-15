using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects;

/// <summary>
/// Domain value object representing password strength analysis result
/// </summary>
public record PasswordStrengthAnalysis(
    PasswordStrength Strength,
    string Description,
    double Score);