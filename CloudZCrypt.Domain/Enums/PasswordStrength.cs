namespace CloudZCrypt.Domain.Enums
{
    /// <summary>
    /// Represents qualitative categories used to classify the overall robustness of a password
    /// based on characteristics such as length, character diversity, entropy and resistance
    /// to common attack strategies (e.g., brute force, dictionary, and pattern-based attacks).
    /// </summary>
    /// <remarks>
    /// This enumeration enables standardized communication of password quality within the
    /// application. Higher values indicate stronger passwords. The evaluation logic that
    /// assigns these values typically considers factors including: minimum length, inclusion
    /// of upper and lower case letters, digits, symbols, absence of repeated or sequential
    /// patterns, and resistance to known-compromised password lists.
    /// </remarks>
    public enum PasswordStrength
    {
        /// <summary>
        /// Indicates a password that provides virtually no security. Usually very short,
        /// lacks character diversity, and can be guessed or brute-forced almost instantly.
        /// </summary>
        VeryWeak,
        /// <summary>
        /// Indicates a password that is still insecure and susceptible to rapid compromise,
        /// but marginally better than <see cref="VeryWeak"/> (e.g., slightly longer or with
        /// minimal variation in characters).
        /// </summary>
        Weak,
        /// <summary>
        /// Indicates a password with acceptable basic characteristics but still below
        /// recommended security standards. May lack sufficient length or entropy for
        /// high-security contexts.
        /// </summary>
        Fair,
        /// <summary>
        /// Indicates a password that meets commonly recommended guidelines (length and
        /// reasonable character diversity) and offers resistance against basic automated
        /// attacks, yet could be improved for sensitive scenarios.
        /// </summary>
        Good,
        /// <summary>
        /// Indicates a password with strong entropy, substantial length, and high resistance
        /// to common attack vectors. Suitable for protecting sensitive data and accounts.
        /// </summary>
        Strong,
    }
}
