namespace HabitFlow.Shared;
public sealed record Error(string Code, string Message)
{ public static Error None => new(string.Empty, string.Empty); }
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    protected Result(bool isSuccess, Error error) { IsSuccess = isSuccess; Error = error; }
    public static Result Success() => new(true, Error.None);
    public static Result Failure(string code, string message) => new(false, new Error(code, message));
}
public sealed class Result<T> : Result
{
    public T? Value { get; }
    private Result(T value) : base(true, Error.None) => Value = value;
    private Result(Error error) : base(false, error) { }
    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string code, string message) => new(new Error(code, message));
}
public static class AppConstants { public const int FreePlanHabitLimit = 5; public const string CompanyName = "MNSOFT"; }
