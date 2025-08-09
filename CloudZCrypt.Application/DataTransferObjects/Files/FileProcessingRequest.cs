using CloudZCrypt.Application.Constants;

namespace CloudZCrypt.Application.DataTransferObjects.FileProcessing
{
    public record FileProcessingRequest(
        string SourceDirectory,
        string DestinationDirectory,
        string Password,
        CryptOperation CryptOperation);
}