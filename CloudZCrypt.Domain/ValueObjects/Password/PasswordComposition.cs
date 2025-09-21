namespace CloudZCrypt.Domain.ValueObjects.Password;

/// <summary>
/// Represents the character category composition of a password (e.g. presence of upper, lower, digit, special and other characters).
/// </summary>
/// <remarks>
/// This value object is typically used in password strength evaluation logic to determine how many distinct
/// character classes are present. The number of categories present can influence scoring or validation rules.
/// </remarks>
/// <param name="HasUpper">Indicates whether the password contains at least one uppercase alphabetical character (A-Z).</param>
/// <param name="HasLower">Indicates whether the password contains at least one lowercase alphabetical character (a-z).</param>
/// <param name="HasDigit">Indicates whether the password contains at least one numeric digit (0-9).</param>
/// <param name="HasSpecial">Indicates whether the password contains at least one special character (punctuation or symbol).</param>
/// <param name="HasOther">Indicates whether the password contains at least one character that does not fall into the standard upper, lower, digit, or typical special sets (e.g. Unicode symbols).</param>
public sealed record PasswordComposition(
    bool HasUpper,
    bool HasLower,
    bool HasDigit,
    bool HasSpecial,
    bool HasOther
)
{
    /// <summary>
    /// Gets the total number of distinct character categories present in the password.
    /// </summary>
    public int CategoryCount =>
        (HasUpper ? 1 : 0)
        + (HasLower ? 1 : 0)
        + (HasDigit ? 1 : 0)
        + (HasSpecial ? 1 : 0)
        + (HasOther ? 1 : 0);
}
