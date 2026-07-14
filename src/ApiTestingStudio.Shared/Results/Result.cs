namespace ApiTestingStudio.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without a return value.
/// </summary>
public readonly record struct Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that yields a value on success.
/// </summary>
public readonly record struct Result<TValue>
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    /// <summary>The success value. Throws if accessed on a failed result.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<TValue> Success(TValue value) => new(true, value, Error.None);

    public static Result<TValue> Failure(Error error) => new(false, default, error);
}
