using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.Password;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudZCrypt.Domain.Services
{
    /// <summary>
    /// Provides services for evaluating password strength and generating random passwords
    /// according to specified composition options.
    /// </summary>
    /// <remarks>
    /// The analysis algorithm estimates entropy based on the effective character pool and length,
    /// then subtracts heuristic penalties for common weaknesses such as repeated characters,
    /// linear sequences, dictionary-like substrings (including basic leet variations), inclusion of
    /// calendar years, and low character class diversity. The resulting score is mapped to a
    /// qualitative <see cref="PasswordStrength"/> classification and accompanied by concise
    /// improvement suggestions. Password generation uses a cryptographically secure random number
    /// generator and honors inclusion / exclusion flags defined via <see cref="PasswordGenerationOptions"/>.
    /// </remarks>
    internal class PasswordService : IPasswordService
    {
        private const double MaxEntropyBits = 120.0;
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string NumberChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        private const string SimilarChars = "il1Lo0O";

        private static readonly Regex UpperCaseRegex = new(@"[A-Z]");
        private static readonly Regex LowerCaseRegex = new(@"[a-z]");
        private static readonly Regex NumberRegex = new(@"[0-9]");
        private static readonly Regex SpecialCharRegex = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        private static readonly Regex YearRegex = new(@"\b(19|20)\d{2}\b");

        private static readonly string[] CommonSubstrings =
        [
            "password",
            "qwerty",
            "admin",
            "user",
            "login",
            "test",
            "guest",
            "root",
            "abc",
            "qwe",
            "letmein",
        ];

        private static readonly string[] LinearSequences =
        [
            "abcdefghijklmnopqrstuvwxyz",
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm",
            "0123456789",
        ];

        private static readonly Dictionary<char, char> LeetMap = new()
        {
            ['0'] = 'o',
            ['1'] = 'l',
            ['3'] = 'e',
            ['4'] = 'a',
            ['5'] = 's',
            ['7'] = 't',
            ['8'] = 'b',
            ['9'] = 'g',
            ['@'] = 'a',
            ['$'] = 's',
            ['!'] = 'i',
        };

        /// <summary>
        /// Analyzes the supplied password and returns an assessment including strength classification,
        /// descriptive feedback, and a normalized score (0–100).
        /// </summary>
        /// <param name="password">The password to evaluate. May be empty or null, which yields a very weak result.</param>
        /// <returns>A <see cref="PasswordStrengthAnalysis"/> describing the evaluated strength and guidance.</returns>
        public PasswordStrengthAnalysis AnalyzePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new PasswordStrengthAnalysis(
                    PasswordStrength.VeryWeak,
                    "Empty password. Please enter a password.",
                    0
                );
            }

            string trimmed = password.Trim();
            int poolSize = EstimatePoolSize(trimmed, out PasswordComposition compositionFlags);
            double baseEntropy = poolSize > 1 ? trimmed.Length * Math.Log2(poolSize) : 0;
            double penaltyBits = 0;

            penaltyBits += RepetitionPenalty(trimmed);
            penaltyBits += SequencePenalty(trimmed);
            penaltyBits += PatternPenalty(trimmed);
            penaltyBits += YearPenalty(trimmed);
            penaltyBits += HomogeneousClassPenalty(compositionFlags, trimmed);

            double entropy = Math.Max(0, baseEntropy - penaltyBits);
            double rawScore = entropy / MaxEntropyBits * 100.0;
            double score = Math.Max(0, Math.Min(100, rawScore));

            if (score < 100 && compositionFlags.CategoryCount >= 4 && trimmed.Length >= 16 && entropy >= 90)
            {
                score = Math.Min(100, score + 5);
            }

            PasswordStrength strength = GetStrengthFromScore(score);
            string description = BuildDescription(strength, score, entropy, compositionFlags, trimmed);

            return new PasswordStrengthAnalysis(strength, description, Math.Round(score, 2));
        }

        /// <summary>
        /// Generates a cryptographically secure random password using the specified length and generation options.
        /// </summary>
        /// <param name="length">Desired length of the password. Must be greater than zero.</param>
        /// <param name="options">Bitwise combination of <see cref="PasswordGenerationOptions"/> controlling included character sets and exclusions.</param>
        /// <returns>A randomly generated password string.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="length"/> is less than or equal to zero, or when <paramref name="options"/> is <see cref="PasswordGenerationOptions.None"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when option filters remove all available characters.</exception>
        public string GeneratePassword(int length, PasswordGenerationOptions options)
        {
            if (length <= 0)
            {
                throw new ArgumentException("Password length must be greater than 0", nameof(length));
            }

            if (options == PasswordGenerationOptions.None)
            {
                throw new ArgumentException("At least one character type must be selected", nameof(options));
            }

            StringBuilder charSet = new();

            if (options.HasFlag(PasswordGenerationOptions.IncludeUppercase))
            {
                charSet.Append(UppercaseChars);
            }

            if (options.HasFlag(PasswordGenerationOptions.IncludeLowercase))
            {
                charSet.Append(LowercaseChars);
            }

            if (options.HasFlag(PasswordGenerationOptions.IncludeNumbers))
            {
                charSet.Append(NumberChars);
            }

            if (options.HasFlag(PasswordGenerationOptions.IncludeSpecialCharacters))
            {
                charSet.Append(SpecialChars);
            }

            string availableChars = charSet.ToString();

            if (options.HasFlag(PasswordGenerationOptions.ExcludeSimilarCharacters))
            {
                availableChars = new string(availableChars.Where(c => !SimilarChars.Contains(c)).ToArray());
            }

            if (string.IsNullOrEmpty(availableChars))
            {
                throw new InvalidOperationException("No characters available for password generation with the given options");
            }

            StringBuilder password = new(length);
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();

            byte[] buffer = new byte[length];
            rng.GetBytes(buffer);

            for (int i = 0; i < length; i++)
            {
                password.Append(availableChars[buffer[i] % availableChars.Length]);
            }

            return password.ToString();
        }

        /// <summary>
        /// Estimates the size of the effective character pool used in a password based on detected character classes.
        /// </summary>
        /// <param name="password">The password whose composition is being analyzed.</param>
        /// <param name="flags">Outputs the detected composition flags describing character class presence.</param>
        /// <returns>The approximate pool size used for entropy calculation.</returns>
        private static int EstimatePoolSize(string password, out PasswordComposition flags)
        {
            bool hasUpper = UpperCaseRegex.IsMatch(password);
            bool hasLower = LowerCaseRegex.IsMatch(password);
            bool hasDigit = NumberRegex.IsMatch(password);
            bool hasSpecial = SpecialCharRegex.IsMatch(password);
            bool hasOther = password.Any(c => c > 127);

            int size = 0;

            if (hasLower)
            {
                size += 26;
            }

            if (hasUpper)
            {
                size += 26;
            }

            if (hasDigit)
            {
                size += 10;
            }

            if (hasSpecial)
            {
                size += 32;
            }

            if (hasOther)
            {
                size += 50;
            }

            flags = new PasswordComposition(hasUpper, hasLower, hasDigit, hasSpecial, hasOther);

            return size;
        }

        /// <summary>
        /// Calculates an entropy penalty based on runs of identical consecutive characters.
        /// </summary>
        /// <param name="password">The password text to inspect for repeated character sequences.</param>
        /// <returns>Penalty value (in bits) to subtract from base entropy.</returns>
        private static double RepetitionPenalty(string password)
        {
            double penalty = 0;
            int runLength = 1;

            for (int i = 1; i < password.Length; i++)
            {
                if (password[i] == password[i - 1])
                {
                    runLength++;
                }
                else
                {
                    if (runLength > 2)
                    {
                        penalty += (runLength - 2) * 1.5;
                    }
                    runLength = 1;
                }
            }

            if (runLength > 2)
            {
                penalty += (runLength - 2) * 1.5;
            }

            return penalty;
        }

        /// <summary>
        /// Calculates a penalty for linear ascending or descending character sequences (alphabetic, keyboard, numeric).
        /// </summary>
        /// <param name="password">The password to evaluate for sequential patterns.</param>
        /// <returns>Penalty value (in bits) reflecting detected sequences.</returns>
        private static double SequencePenalty(string password)
        {
            double penalty = 0;
            string lower = password.ToLowerInvariant();

            foreach (string seq in LinearSequences)
            {
                penalty += SequenceScan(lower, seq);
                string rev = new(seq.Reverse().ToArray());
                penalty += SequenceScan(lower, rev);
            }

            return penalty;
        }

        /// <summary>
        /// Scans the password for occurrences of a provided linear sequence and returns a cumulative penalty.
        /// </summary>
        /// <param name="passwordLower">Lower-cased password being analyzed.</param>
        /// <param name="sequence">Canonical sequence to search for (e.g., alphabet fragment).</param>
        /// <returns>Accumulated penalty for all matches of length three or greater.</returns>
        private static double SequenceScan(string passwordLower, string sequence)
        {
            double penalty = 0;

            for (int i = 0; i <= passwordLower.Length - 3; i++)
            {
                int max = Math.Min(sequence.Length, passwordLower.Length - i);
                int len = 0;

                for (int j = 0; j < max; j++)
                {
                    if (passwordLower[i + j] == sequence[j])
                    {
                        len++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (len >= 3)
                {
                    penalty += (len - 2) * 2.0;
                }
            }

            return penalty;
        }

        /// <summary>
        /// Calculates a penalty for common dictionary-like substrings and their leet-normalized equivalents.
        /// </summary>
        /// <param name="password">The password examined for common weak substrings.</param>
        /// <returns>Penalty (in bits) applied for each matching substring.</returns>
        private static double PatternPenalty(string password)
        {
            double penalty = 0;
            string lower = password.ToLowerInvariant();

            penalty = CommonSubstrings.Where(lower.Contains).Sum(p => 6);
            string canon = NormalizeLeet(lower);
            penalty += CommonSubstrings.Where(canon.Contains).Sum(p => 6);

            return penalty;
        }

        /// <summary>
        /// Calculates a penalty for four-digit years commonly used in passwords.
        /// </summary>
        /// <param name="password">The password text being inspected.</param>
        /// <returns>Penalty (in bits) equal to a fixed amount per detected year.</returns>
        private static double YearPenalty(string password)
        {
            return YearRegex.Matches(password).Count * 4.0;
        }

        /// <summary>
        /// Applies a penalty when the password exhibits low diversity in character classes.
        /// </summary>
        /// <param name="flags">Composition flags indicating which character categories are present.</param>
        /// <param name="password">Original password text (used for length-based scaling).</param>
        /// <returns>Penalty (in bits) based on category count and length.</returns>
        private static double HomogeneousClassPenalty(PasswordComposition flags, string password)
        {
            return flags.CategoryCount <= 1 ? Math.Min(20, password.Length * 2)
                : flags.CategoryCount == 2 && password.Length < 10 ? 10
                : 0;
        }

        /// <summary>
        /// Normalizes common leet-speak substitutions to their alphabetic counterparts for pattern detection.
        /// </summary>
        /// <param name="input">Password text to normalize.</param>
        /// <returns>String with leet characters replaced by canonical letters where applicable.</returns>
        private static string NormalizeLeet(string input)
        {
            StringBuilder sb = new(input.Length);

            foreach (char c in input)
            {
                if (LeetMap.TryGetValue(c, out char mapped))
                {
                    sb.Append(mapped);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Maps a numeric score to a qualitative <see cref="PasswordStrength"/> classification.
        /// </summary>
        /// <param name="score">Normalized score in the range 0–100.</param>
        /// <returns>The corresponding <see cref="PasswordStrength"/> value.</returns>
        private static PasswordStrength GetStrengthFromScore(double score)
        {
            return score switch
            {
                >= 85 => PasswordStrength.Strong,
                >= 65 => PasswordStrength.Good,
                >= 45 => PasswordStrength.Fair,
                >= 25 => PasswordStrength.Weak,
                _ => PasswordStrength.VeryWeak,
            };
        }

        /// <summary>
        /// Builds a human-readable description summarizing the password strength, entropy, and improvement suggestions.
        /// </summary>
        /// <param name="strength">Computed qualitative strength classification.</param>
        /// <param name="score">Normalized strength score (0–100).</param>
        /// <param name="entropy">Estimated entropy (in bits) after penalties.</param>
        /// <param name="flags">Composition flags indicating character class usage.</param>
        /// <param name="password">Original password string used for contextual suggestions.</param>
        /// <returns>Formatted description string containing classification, entropy, and suggestions.</returns>
        private static string BuildDescription(
            PasswordStrength strength,
            double score,
            double entropy,
            PasswordComposition flags,
            string password
        )
        {
            StringBuilder sb = new();
            sb.Append(
                strength switch
                {
                    PasswordStrength.VeryWeak => "Very Weak",
                    PasswordStrength.Weak => "Weak",
                    PasswordStrength.Fair => "Fair",
                    PasswordStrength.Good => "Good",
                    PasswordStrength.Strong => "Strong",
                    _ => "?",
                }
            );

            sb.Append($" // Entropy: {entropy:0.0} bits");

            List<string> tips = [];

            if (password.Length < 12)
            {
                tips.Add("Increase length (≥12 chars)");
            }

            if (!flags.HasUpper)
            {
                tips.Add("Add uppercase letters");
            }

            if (!flags.HasLower)
            {
                tips.Add("Add lowercase letters");
            }

            if (!flags.HasDigit)
            {
                tips.Add("Add digits");
            }

            if (!flags.HasSpecial)
            {
                tips.Add("Add symbols");
            }

            if (flags.CategoryCount < 4 && password.Length < 16)
            {
                tips.Add("Use more character variety");
            }

            if (HasObviousSequence(password))
            {
                tips.Add("Avoid sequences (e.g. abc, 123, qwerty)");
            }

            if (HasRepeats(password))
            {
                tips.Add("Reduce repeated characters");
            }

            if (YearRegex.IsMatch(password))
            {
                tips.Add("Avoid years");
            }

            if (tips.Count > 0)
            {
                sb.Append(" // Suggestions: ");
                sb.Append(string.Join(", ", tips.Take(3)));
            }
            else if (strength == PasswordStrength.Strong)
            {
                sb.Append(" // Good job!");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the password contains obvious sequential fragments (forward or reverse).
        /// </summary>
        /// <param name="password">The password to inspect.</param>
        /// <returns>true if a sequence is detected; otherwise false.</returns>
        private static bool HasObviousSequence(string password)
        {
            string lower = password.ToLowerInvariant();

            return LinearSequences.Any(seq => lower.Contains(seq[..Math.Min(seq.Length, 4)]))
                || LinearSequences.Any(seq =>
                {
                    string rev = new(seq.Reverse().ToArray());
                    return lower.Contains(rev[..Math.Min(rev.Length, 4)]);
                });
        }

        /// <summary>
        /// Determines whether the password contains any immediate consecutive repeated characters.
        /// </summary>
        /// <param name="password">The password to evaluate.</param>
        /// <returns>true if at least one repeated consecutive character pair is found; otherwise false.</returns>
        private static bool HasRepeats(string password)
        {
            for (int i = 1; i < password.Length; i++)
            {
                if (password[i] == password[i - 1])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
