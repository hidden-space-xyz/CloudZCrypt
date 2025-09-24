using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.ValueObjects.Password;

namespace CloudZCrypt.Domain.Services.Interfaces
{
    /// <summary>
    /// Defines operations related to password analysis and generation used throughout the domain layer.
    /// </summary>
    /// <remarks>
    /// Implementations are responsible for evaluating the qualitative strength of supplied passwords and for
    /// generating new passwords that satisfy caller-specified composition requirements (length, character sets,
    /// exclusion rules, etc.). The analysis typically considers factors such as length, character diversity,
    /// repetition, sequences, and optionally external breach / dictionary lookups. Password generation must honor
    /// the provided <see cref="PasswordGenerationOptions"/> flags and should apply consistent rules for excluding
    /// visually similar characters when <see cref="PasswordGenerationOptions.ExcludeSimilarCharacters"/> is set.
    /// </remarks>
    public interface IPasswordService
    {
        /// <summary>
        /// Analyzes the supplied password and returns a structured assessment of its strength.
        /// </summary>
        /// <param name="password">The password to evaluate. Must not be null or empty.</param>
        /// <returns>A <see cref="PasswordStrengthAnalysis"/> instance containing strength classification and related metrics.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is null, empty, or composed solely of whitespace.</exception>
        PasswordStrengthAnalysis AnalyzePasswordStrength(string password);

        /// <summary>
        /// Generates a password that satisfies the requested length and composition options.
        /// </summary>
        /// <param name="length">The desired length of the password. Must be a positive integer and meet any implementation-specific minimum.</param>
        /// <param name="options">A bitwise combination of <see cref="PasswordGenerationOptions"/> flags that dictate which character sets to include and whether to exclude similar characters.</param>
        /// <returns>A newly generated password string meeting the specified criteria.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is less than or equal to zero, or below a required minimum.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="options"/> is <see cref="PasswordGenerationOptions.None"/> or does not include any valid character inclusion flags.</exception>
        string GeneratePassword(int length, PasswordGenerationOptions options);
    }
}
