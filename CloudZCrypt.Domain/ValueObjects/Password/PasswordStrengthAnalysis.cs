using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Domain.ValueObjects.Password;

/// <summary>
/// Represents the evaluated strength characteristics of a password, combining a qualitative
/// classification with a textual explanation and a numerical score for finer granularity.
/// </summary>
/// <remarks>
/// This value object is typically produced by password analysis logic that inspects factors
/// such as length, character diversity (uppercase, lowercase, digits, symbols), entropy,
/// avoidance of common patterns (repetition, sequences, keyboard walks), and resistance to
/// dictionary or brute-force attacks. The <see cref="Strength"/> property conveys a high-level
/// category, while <see cref="Description"/> provides human-readable guidance for end users and
/// <see cref="Score"/> supplies a numeric indicator useful for UI progress meters or threshold-based
/// decisions.
/// </remarks>
/// <param name="Strength">The qualitative classification of the password's robustness.</param>
/// <param name="Description">A human-readable explanation of why the password received its assigned classification and/or recommendations for improvement.</param>
/// <param name="Score">A continuous numeric score (higher indicates stronger) that supports fine-grained comparison or threshold evaluation. Typical implementations normalize this to a bounded range (e.g., 0–100 or 0–1).</param>
public sealed record PasswordStrengthAnalysis(
    PasswordStrength Strength,
    string Description,
    double Score
);
