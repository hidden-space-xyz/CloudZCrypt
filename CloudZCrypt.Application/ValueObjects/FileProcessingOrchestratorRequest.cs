using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.ValueObjects;

/// <summary>
/// Represents the immutable set of parameters required to perform a file processing operation.
/// </summary>
/// <param name="SourcePath">The path to the source file or directory to be processed. Must exist and be accessible.</param>
/// <param name="DestinationPath">The target file or directory path where output will be written. Parent directory should be writable.</param>
/// <param name="Password">The primary password used for key derivation during encryption or decryption.</param>
/// <param name="ConfirmPassword">A confirmation of <paramref name="Password"/> to guard against input mistakes when encrypting.</param>
/// <param name="EncryptionAlgorithm">The symmetric encryption algorithm to apply (e.g., AES, Twofish).</param>
/// <param name="KeyDerivationAlgorithm">The key derivation function (e.g., Argon2id, PBKDF2) used to derive cryptographic keys from the password.</param>
/// <param name="Operation">Indicates whether the request is for encryption or decryption.</param>
/// <param name="NameObfuscation">Indicates the filename obfuscation mode to apply during encryption and to restore during decryption.</param>
/// <remarks>
/// This record is a simple data carrier and performs no intrinsic validation. Use <see cref="IFileProcessingOrchestrator.ValidateAsync"/>
/// to verify correctness and <see cref="IFileProcessingOrchestrator.AnalyzeWarningsAsync"/> for advisory checks before execution.
/// </remarks>
public sealed record FileProcessingOrchestratorRequest(
    string SourcePath,
    string DestinationPath,
    string Password,
    string ConfirmPassword,
    EncryptionAlgorithm EncryptionAlgorithm,
    KeyDerivationAlgorithm KeyDerivationAlgorithm,
    EncryptOperation Operation,
    NameObfuscationMode NameObfuscation
);
