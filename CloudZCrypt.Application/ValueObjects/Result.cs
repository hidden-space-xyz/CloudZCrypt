namespace CloudZCrypt.Application.ValueObjects;

/// <summary>
/// Represents the outcome of an operation, encapsulating success or failure state and any associated error messages.
/// </summary>
/// <remarks>
/// This non-generic result type is useful when an operation does not return a value but still needs to communicate
/// whether it succeeded and, if not, why it failed.
/// <para>Usage example:
/// <code>
/// var result = service.Execute();
/// if (result.IsFailure)
/// {
///     // Handle errors (result.Errors)
/// }
/// </code>
/// </para>
/// </remarks>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">A value indicating whether the operation succeeded.</param>
    /// <param name="errors">An array of error messages associated with a failed operation. Should be empty when <paramref name="isSuccess"/> is <c>true</c>.</param>
    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the collection of error messages associated with a failed operation. Returns an empty array when the operation succeeds.
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Creates a successful <see cref="Result"/> instance.
    /// </summary>
    /// <returns>A successful result with no errors.</returns>
    public static Result Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed <see cref="Result"/> instance with one or more error messages.
    /// </summary>
    /// <param name="errors">The error messages describing the failure. Must contain at least one value.</param>
    /// <returns>A failed result containing the specified errors.</returns>
    public static Result Failure(params string[] errors) => new(false, errors);

    /// <summary>
    /// Creates a failed <see cref="Result"/> instance from an enumerable collection of error messages.
    /// </summary>
    /// <param name="errors">The error messages describing the failure. Must not be <c>null</c>.</param>
    /// <returns>A failed result containing the specified errors.</returns>
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToArray());

    /// <summary>
    /// Implicitly converts a single error message into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="error">The error message representing the failure.</param>
    /// <returns>A failed result containing the provided error message.</returns>
    public static implicit operator Result(string error) => Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value when successful, or one or more errors when it fails.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
/// <remarks>
/// Use this type when you need to propagate both a potential value and error information without throwing exceptions for flow control.
/// <para>Usage example:
/// <code>
/// Result&lt;User&gt; result = userService.GetUser(id);
/// if (result.IsSuccess)
/// {
///     var user = result.Value;
/// }
/// else
/// {
///     // Inspect result.Errors
/// }
/// </code>
/// </para>
/// </remarks>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value produced by the operation if successful; otherwise the default value of <typeparamref name="T"/>.</param>
    /// <param name="isSuccess">A value indicating whether the operation succeeded.</param>
    /// <param name="errors">An array of error messages associated with a failed operation. Should be empty when <paramref name="isSuccess"/> is <c>true</c>.</param>
    protected Result(T value, bool isSuccess, string[] errors) : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value produced by the operation when it is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to access the value of a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of failed result");

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/> instance containing the specified value.
    /// </summary>
    /// <param name="value">The value produced by the successful operation.</param>
    /// <returns>A successful result containing the provided value.</returns>
    public static Result<T> Success(T value) => new(value, true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> instance with one or more error messages.
    /// </summary>
    /// <param name="errors">The error messages describing the failure. Must contain at least one value.</param>
    /// <returns>A failed result containing the specified errors.</returns>
    public static new Result<T> Failure(params string[] errors) => new(default!, false, errors);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> instance from an enumerable collection of error messages.
    /// </summary>
    /// <param name="errors">The error messages describing the failure. Must not be <c>null</c>.</param>
    /// <returns>A failed result containing the specified errors.</returns>
    public static new Result<T> Failure(IEnumerable<string> errors) => new(default!, false, errors.ToArray());

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> into a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The value representing a successful result.</param>
    /// <returns>A successful result containing the specified value.</returns>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts a single error message into a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="error">The error message representing the failure.</param>
    /// <returns>A failed result containing the provided error message.</returns>
    public static implicit operator Result<T>(string error) => Failure(error);
}
