using CloudZCrypt.Domain.Constants;

namespace CloudZCrypt.Application.DataTransferObjects.Files;

public record FileProcessingRequest(
    string SourceDirectory,
    string DestinationDirectory,
    string Password,
    EncryptOperation EncryptOperation,
    EncryptionAlgorithm EncryptionAlgorithm,
    KeyDerivationAlgorithm KeyDerivationAlgorithm);