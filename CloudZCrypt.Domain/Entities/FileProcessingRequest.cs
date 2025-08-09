using CloudZCrypt.Domain.Constants;

namespace CloudZCrypt.Domain.Entities;

public record FileProcessingRequest(
    string SourceDirectory,
    string DestinationDirectory,
    string Password,
    EncryptOperation EncryptOperation,
    EncryptionAlgorithm EncryptionAlgorithm);