using System.Diagnostics.CodeAnalysis;

namespace AegiDocs.Application;

/// <summary>
/// Represents the outcome of an application operation without a return value.
/// </summary>
public sealed class Result
{
    private Result(ApplicationError? error)
    {
        Error = error;
    }

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets whether the operation completed with an expected failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error for a failed operation, or <see langword="null" /> for success.
    /// </summary>
    public ApplicationError? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(error: null);

    /// <summary>
    /// Creates a failed result from an expected application error.
    /// </summary>
    public static Result Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(error);
    }
}
