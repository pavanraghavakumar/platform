namespace Hospital.Application.Common;

public enum ResultStatus
{
    Success,
    Validation,
    Conflict,
    Unavailable,
    NotFound,
    Forbidden
}

public record Result(ResultStatus Status, string? Error = null)
{
    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new(ResultStatus.Success);
    public static Result Failure(ResultStatus status, string error) => new(status, error);
}

#pragma warning disable CA1000 // Result factories intentionally live beside their generic result type.
public sealed record Result<T>(ResultStatus Status, T? Value = default, string? Error = null)
    : Result(Status, Error)
{
    public static Result<T> Success(T value) => new(ResultStatus.Success, value);
    public static new Result<T> Failure(ResultStatus status, string error) => new(status, default, error);
}
#pragma warning restore CA1000
