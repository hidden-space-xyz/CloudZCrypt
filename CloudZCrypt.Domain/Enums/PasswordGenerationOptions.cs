namespace CloudZCrypt.Domain.Enums;

/// <summary>
/// Defines the available options that control how a password is generated.
/// </summary>
/// <remarks>
/// This enumeration is decorated with the <see cref="System.FlagsAttribute"/>, allowing a bitwise combination of its
/// members to specify multiple password generation behaviors simultaneously. Typical usage involves combining values
/// with the bitwise OR operator (e.g., IncludeUppercase | IncludeNumbers | ExcludeSimilarCharacters).
/// </remarks>
[Flags]
public enum PasswordGenerationOptions
{
    /// <summary>
    /// No password generation options are selected. Implementations should treat this as an invalid or default state
    /// and typically require at least one inclusion flag (e.g., letters, numbers, or special characters).
    /// </summary>
    None = 0,

    /// <summary>
    /// Include uppercase Latin alphabet characters (A–Z) in the generated password.
    /// </summary>
    IncludeUppercase = 1,

    /// <summary>
    /// Include lowercase Latin alphabet characters (a–z) in the generated password.
    /// </summary>
    IncludeLowercase = 2,

    /// <summary>
    /// Include numeric characters (0–9) in the generated password.
    /// </summary>
    IncludeNumbers = 4,

    /// <summary>
    /// Include special (non-alphanumeric) characters in the generated password. The exact character set (e.g., !@#$%&*)
    /// should be defined and documented by the password generation service implementation.
    /// </summary>
    IncludeSpecialCharacters = 8,

    /// <summary>
    /// Exclude characters that are visually similar and prone to human misinterpretation (e.g., O/0, I/l/1). This option
    /// refines the character pool after other inclusion flags have been applied.
    /// </summary>
    ExcludeSimilarCharacters = 16,
}
