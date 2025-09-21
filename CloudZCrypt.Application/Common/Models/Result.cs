namespace CloudZCrypt.Application.Common.Models;

public class Result
{
    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string[] Errors { get; }

    public static Result Success() => new(true, Array.Empty<string>());

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToArray());

    public static implicit operator Result(string error) => Failure(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected Result(T value, bool isSuccess, string[] errors) : base(isSuccess, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of failed result");

    public static Result<T> Success(T value) => new(value, true, Array.Empty<string>());

    public static new Result<T> Failure(params string[] errors) => new(default!, false, errors);

    public static new Result<T> Failure(IEnumerable<string> errors) => new(default!, false, errors.ToArray());

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(string error) => Failure(error);
}
