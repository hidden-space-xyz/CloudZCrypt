using CloudZCrypt.Domain.Enums;

namespace CloudZCrypt.Application.ValueObjects;

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
