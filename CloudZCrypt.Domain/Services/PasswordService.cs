using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudZCrypt.Domain.Services
{
    internal class PasswordService : IPasswordService
    {
        // Character class regexes
        private static readonly Regex UpperCaseRegex = new(@"[A-Z]");
        private static readonly Regex LowerCaseRegex = new(@"[a-z]");
        private static readonly Regex NumberRegex = new(@"[0-9]");
        private static readonly Regex SpecialCharRegex = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        private static readonly Regex YearRegex = new(@"\b(19|20)\d{2}\b");

        // Common weak substrings and keyboard sequences
        private static readonly string[] CommonSubstrings =
        [
            "password","qwerty","admin","user","login","test","guest","root","abc","qwe","letmein"
        ];

        // Keyboard / natural sequences to detect inside the password (treated both directions)
        private static readonly string[] LinearSequences =
        [
            "abcdefghijklmnopqrstuvwxyz",
            "qwertyuiop","asdfghjkl","zxcvbnm",
            "0123456789"
        ];

        // Leet speak normalization map (used to detect disguised common words)
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
            ['!'] = 'i'
        };

        // Upper bound of entropy (in bits) that we map to 100 score.
        private const double MaxEntropyBits = 120.0;

        // Character sets for password generation
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string NumberChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        private const string SimilarChars = "il1Lo0O";

        public PasswordStrengthAnalysis AnalyzePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new PasswordStrengthAnalysis(
                    PasswordStrength.VeryWeak,
                    "Empty password. Please enter a password.",
                    0);
            }

            string trimmed = password.Trim();

            // Character set size estimation
            int poolSize = EstimatePoolSize(trimmed, out PasswordComposition compositionFlags);

            // Base entropy estimation: length * log2(poolSize)
            double baseEntropy = poolSize > 1
                ? trimmed.Length * Math.Log2(poolSize)
                : 0;

            // Penalties (in entropy bits) for weaknesses
            double penaltyBits = 0;

            penaltyBits += RepetitionPenalty(trimmed);
            penaltyBits += SequencePenalty(trimmed);
            penaltyBits += PatternPenalty(trimmed);
            penaltyBits += YearPenalty(trimmed);
            penaltyBits += HomogeneousClassPenalty(compositionFlags, trimmed);

            double entropy = Math.Max(0, baseEntropy - penaltyBits);

            // Convert entropy (0..MaxEntropyBits+) to 0..100 capped
            double rawScore = entropy / MaxEntropyBits * 100.0;
            double score = Math.Max(0, Math.Min(100, rawScore));

            // Additional soft boosts for very strong diversified passwords
            if (score < 100 && compositionFlags.CategoryCount >= 4 && trimmed.Length >= 16 && entropy >= 90)
            {
                score = Math.Min(100, score + 5);
            }

            PasswordStrength strength = GetStrengthFromScore(score);
            string description = BuildDescription(strength, score, entropy, compositionFlags, trimmed);

            return new PasswordStrengthAnalysis(strength, description, Math.Round(score, 2));
        }

        public string GeneratePassword(int length, PasswordGenerationOptions options)
        {
            if (length <= 0)
                throw new ArgumentException("Password length must be greater than 0", nameof(length));

            if (options == PasswordGenerationOptions.None)
                throw new ArgumentException("At least one character type must be selected", nameof(options));

            // Build character set based on options
            StringBuilder charSet = new();

            if (options.HasFlag(PasswordGenerationOptions.IncludeUppercase))
                charSet.Append(UppercaseChars);

            if (options.HasFlag(PasswordGenerationOptions.IncludeLowercase))
                charSet.Append(LowercaseChars);

            if (options.HasFlag(PasswordGenerationOptions.IncludeNumbers))
                charSet.Append(NumberChars);

            if (options.HasFlag(PasswordGenerationOptions.IncludeSpecialCharacters))
                charSet.Append(SpecialChars);

            string availableChars = charSet.ToString();

            if (options.HasFlag(PasswordGenerationOptions.ExcludeSimilarCharacters))
            {
                availableChars = new string(availableChars.Where(c => !SimilarChars.Contains(c)).ToArray());
            }

            if (string.IsNullOrEmpty(availableChars))
                throw new InvalidOperationException("No characters available for password generation with the given options");

            // Generate password
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

        private static int EstimatePoolSize(string password, out PasswordComposition flags)
        {
            bool hasUpper = UpperCaseRegex.IsMatch(password);
            bool hasLower = LowerCaseRegex.IsMatch(password);
            bool hasDigit = NumberRegex.IsMatch(password);
            bool hasSpecial = SpecialCharRegex.IsMatch(password);

            // Detect any other unicode categories beyond basic ASCII (bonus diversity)
            bool hasOther = password.Any(c => c > 127);

            int size = 0;
            if (hasLower) size += 26;
            if (hasUpper) size += 26;
            if (hasDigit) size += 10;

            // Approximation of typical symbol count (subset used in regex above)
            if (hasSpecial) size += 32;

            // Add a small pool increment for other Unicode characters (very rough)
            if (hasOther) size += 50;

            flags = new PasswordComposition(hasUpper, hasLower, hasDigit, hasSpecial, hasOther);

            return size;
        }

        private static double RepetitionPenalty(string password)
        {
            // Penalize consecutive repeated characters: each extra duplicate costs bits
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
                        penalty += (runLength - 2) * 1.5; // mild penalty per extra repeat
                    runLength = 1;
                }
            }
            if (runLength > 2)
                penalty += (runLength - 2) * 1.5;

            return penalty;
        }

        private static double SequencePenalty(string password)
        {
            // Detect linear increasing or decreasing sequences of length >= 3 based on predefined sequences
            double penalty = 0;
            string lower = password.ToLowerInvariant();

            foreach (string seq in LinearSequences)
            {
                penalty += SequenceScan(lower, seq);
                // Also reverse direction
                string rev = new(seq.Reverse().ToArray());
                penalty += SequenceScan(lower, rev);
            }

            return penalty;
        }

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
                        len++;
                    else
                        break;
                }

                if (len >= 3)
                {
                    // Penalize sequences: longer sequences cost more
                    penalty += (len - 2) * 2.0;
                }
            }
            return penalty;
        }

        private static double PatternPenalty(string password)
        {
            double penalty = 0;

            string lower = password.ToLowerInvariant();
            penalty = CommonSubstrings.Where(lower.Contains).Sum(p => 6);

            // Leet normalized check
            string canon = NormalizeLeet(lower);
            penalty += CommonSubstrings.Where(canon.Contains).Sum(p => 6);

            return penalty;
        }

        private static double YearPenalty(string password)
        {
            // Years are very predictable -> small penalty per year occurrence
            return YearRegex.Matches(password).Count * 4.0;
        }

        private static double HomogeneousClassPenalty(PasswordComposition flags, string password)
        {
            // If password uses only one class, strong penalty
            return flags.CategoryCount <= 1 ? Math.Min(20, password.Length * 2) : flags.CategoryCount == 2 && password.Length < 10 ? 10 : 0;
        }

        private static string NormalizeLeet(string input)
        {
            StringBuilder sb = new(input.Length);
            foreach (char c in input)
            {
                if (LeetMap.TryGetValue(c, out char mapped))
                    sb.Append(mapped);
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static PasswordStrength GetStrengthFromScore(double score)
        {
            return score switch
            {
                >= 85 => PasswordStrength.Strong,
                >= 65 => PasswordStrength.Good,
                >= 45 => PasswordStrength.Fair,
                >= 25 => PasswordStrength.Weak,
                _ => PasswordStrength.VeryWeak
            };
        }

        private static string BuildDescription(PasswordStrength strength, double score, double entropy, PasswordComposition flags, string password)
        {
            StringBuilder sb = new();
            sb.Append(strength switch
            {
                PasswordStrength.VeryWeak => "Very Weak",
                PasswordStrength.Weak => "Weak",
                PasswordStrength.Fair => "Fair",
                PasswordStrength.Good => "Good",
                PasswordStrength.Strong => "Strong",
                _ => "?"
            });

            sb.Append($" // Entropy: {entropy:0.0} bits");

            List<string> tips = [];

            if (password.Length < 12) tips.Add("Increase length (≥12 chars)");
            if (!flags.HasUpper) tips.Add("Add uppercase letters");
            if (!flags.HasLower) tips.Add("Add lowercase letters");
            if (!flags.HasDigit) tips.Add("Add digits");
            if (!flags.HasSpecial) tips.Add("Add symbols");
            if (flags.CategoryCount < 4 && password.Length < 16) tips.Add("Use more character variety");
            if (HasObviousSequence(password)) tips.Add("Avoid sequences (e.g. abc, 123, qwerty)");
            if (HasRepeats(password)) tips.Add("Reduce repeated characters");
            if (YearRegex.IsMatch(password)) tips.Add("Avoid years");

            if (tips.Count > 0)
            {
                sb.Append(" // Suggestions: ");
                // Append only the first 3 tips for brevity
                sb.Append(string.Join(", ", tips.Take(3)));
            }
            else if (strength == PasswordStrength.Strong)
            {
                sb.Append(" // Good job!");
            }

            return sb.ToString();
        }

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

        private static bool HasRepeats(string password)
        {
            for (int i = 1; i < password.Length; i++)
            {
                if (password[i] == password[i - 1])
                    return true;
            }
            return false;
        }
    }
}
