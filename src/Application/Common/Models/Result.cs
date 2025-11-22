namespace MultiTenantIdentityApi.Application.Common.Models;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static Result Success(string? message = null) =>
        new() { Succeeded = true, Message = message };

    public static Result Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors };

    public static Result Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };
}

/// <summary>
/// Represents the result of an operation with data
/// </summary>
public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data, string? message = null) =>
        new() { Succeeded = true, Data = data, Message = message };

    public new static Result<T> Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors };

    public new static Result<T> Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };
}
