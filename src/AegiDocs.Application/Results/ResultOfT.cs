using System.Diagnostics.CodeAnalysis;

namespace AegiDocs.Application;

/// <summary>
/// Represents the outcome of an application operation with a value on success.
/// </summary>
/// <typeparam name="T">The type of value produced by the operation.</typeparam>
public sealed class Result<T>
{
    private Result(T? value, ApplicationError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets whether the operation completed with an expected failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value for a successful operation, or <see langword="default" /> for failure.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error for a failed operation, or <see langword="null" /> for success.
    /// </summary>
    public ApplicationError? Error { get; }

    /// <summary>
    /// Creates a successful result with a non-null value.
    /// </summary>
#pragma warning disable CA1000 // Static factories keep Result<T>'s valid states explicit at call sites.
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(value, error: null);
    }

    /// <summary>
    /// Creates a failed result from an expected application error.
    /// </summary>
    public static Result<T> Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(value: default, error);
    }
#pragma warning restore CA1000
}
