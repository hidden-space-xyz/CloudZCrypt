namespace CloudZCrypt.Domain.DataTransferObjects.Passwords;


public record PasswordCompositionFlags
{
    public bool HasUpper { get; set; }
    public bool HasLower { get; set; }
    public bool HasDigit { get; set; }
    public bool HasSpecial { get; set; }
    public bool HasOther { get; set; }

    public int CategoryCount =>
        (HasUpper ? 1 : 0) +
        (HasLower ? 1 : 0) +
        (HasDigit ? 1 : 0) +
        (HasSpecial ? 1 : 0) +
        (HasOther ? 1 : 0);
}