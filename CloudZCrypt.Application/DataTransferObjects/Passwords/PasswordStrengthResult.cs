using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.DataTransferObjects.Passwords;

/// <summary>
/// Represents the outcome of a password strength evaluation, including the qualitative strength classification,
/// a human-readable description, and a numeric score that can be used for further programmatic decisions.
/// </summary>
/// <remarks>
/// This data transfer object is typically produced by password analysis services and consumed by UI layers
/// to provide feedback to users. The <c>Score</c> value is expected to be a normalized numeric representation
/// (e.g., on a 0–1 or 0–100 scale) of the evaluated password strength; callers should interpret it consistently
/// with the producing component's contract.
/// </remarks>
/// <param name="Strength">The categorical strength assessment derived from the analyzed password.</param>
/// <param name="Description">A localized or user-friendly explanation describing the password's evaluated strength and potential improvements.</param>
/// <param name="Score">The numeric score associated with the password's strength; higher values indicate stronger passwords.</param>
public record PasswordStrengthResult(PasswordStrength Strength, string Description, double Score);
