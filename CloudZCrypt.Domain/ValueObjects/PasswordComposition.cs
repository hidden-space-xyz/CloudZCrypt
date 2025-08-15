namespace CloudZCrypt.Domain.ValueObjects;

/// <summary>
/// Domain value object representing password composition analysis
/// </summary>
public record PasswordComposition(bool HasUpper, bool HasLower, bool HasDigit, bool HasSpecial, bool HasOther)
{
    public int CategoryCount => (HasUpper ? 1 : 0) + (HasLower ? 1 : 0) + (HasDigit ? 1 : 0) + (HasSpecial ? 1 : 0) + (HasOther ? 1 : 0);
}