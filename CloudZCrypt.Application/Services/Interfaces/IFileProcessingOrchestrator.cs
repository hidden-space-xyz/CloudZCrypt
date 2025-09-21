using CloudZCrypt.Application.ValueObjects;
using CloudZCrypt.Domain.ValueObjects.FileProcessing;

namespace CloudZCrypt.Application.Services.Interfaces;

/// <summary>
/// Defines the contract for orchestrating end-to-end file encryption and decryption workflows.
/// </summary>
/// <remarks>
/// An implementation coordinates validation, pre–execution analysis (warnings), and the actual
/// processing phase (encryption or decryption), while emitting progress updates and returning
/// a rich result model describing the outcome of the operation.
/// Use <see cref="ValidateAsync"/> to perform fail-fast validation, <see cref="AnalyzeWarningsAsync"/>
/// to surface non-fatal advisory messages, and <see cref="ExecuteAsync"/> to run the full pipeline.
/// </remarks>
public interface IFileProcessingOrchestrator
{
    /// <summary>
    /// Validates the supplied request for correctness before any processing occurs.
    /// </summary>
    /// <param name="request">The file processing request containing source/destination paths, credentials, and algorithm selections.</param>
    /// <param name="cancellationToken">A token that can be used to observe cancellation. If cancellation is requested the method may throw <see cref="OperationCanceledException"/>.</param>
    /// <returns>
    /// A read-only list of validation error messages. The collection is empty when the request is valid.
    /// </returns>
    /// <remarks>
    /// Typical validations include path existence, write permissions, password rules, and algorithm compatibility.
    /// This method performs only inexpensive checks and does not create or overwrite any files.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    Task<IReadOnlyList<string>> ValidateAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Analyzes the request for non-fatal conditions and potential usability warnings.
    /// </summary>
    /// <param name="request">The file processing request to inspect.</param>
    /// <param name="cancellationToken">A token to observe cancellation. May cause an <see cref="OperationCanceledException"/> to be thrown.</param>
    /// <returns>
    /// A read-only list of warning messages. The collection is empty when no advisory conditions are detected.
    /// </returns>
    /// <remarks>
    /// This step is intended to inform the user about suboptimal configurations (e.g., weak password strength
    /// or unusually large batch sizes) without preventing execution.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    Task<IReadOnlyList<string>> AnalyzeWarningsAsync(
        FileProcessingOrchestratorRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Executes the full file processing workflow (encryption or decryption) for the specified request.
    /// </summary>
    /// <param name="request">The file processing request that defines source and destination paths, cryptographic algorithms, and password material.</param>
    /// <param name="progress">An optional progress reporter that receives periodic <see cref="FileProcessingStatus"/> updates during execution.</param>
    /// <param name="cancellationToken">A token used to cancel the operation cooperatively. If canceled, an <see cref="OperationCanceledException"/> may be thrown.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> whose value is a <see cref="FileProcessingResult"/> describing aggregate metrics and errors. When the result indicates failure, inspect <see cref="Result.Errors"/> for details.
    /// </returns>
    /// <remarks>
    /// Implementations should stream data efficiently, apply the selected key derivation and encryption algorithms,
    /// and ensure sensitive material is not unnecessarily retained in memory. Partial successes (some files failed)
    /// should be reflected in the returned <see cref="FileProcessingResult"/>.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="cancellationToken"/>.</exception>
    Task<Result<FileProcessingResult>> ExecuteAsync(
        FileProcessingOrchestratorRequest request,
        IProgress<FileProcessingStatus> progress,
        CancellationToken cancellationToken = default
    );
}
