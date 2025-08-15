namespace CloudZCrypt.Domain.DataTransferObjects.Passwords;

/// <summary>
/// Flags for password composition options
/// </summary>
[Flags]
public enum PasswordCompositionOptions
{
    None = 0,
    IncludeUppercase = 1,
    IncludeLowercase = 2,
    IncludeNumbers = 4,
    IncludeSpecialCharacters = 8,
    ExcludeSimilarCharacters = 16
}