namespace AegiDocs.Application;

/// <summary>
/// Categorizes an expected application failure without exposing implementation details.
/// </summary>
public enum ApplicationErrorCode
{
    Validation,
    InputOutput,
    Capture,
    Permission,
    Cancelled,
    Unexpected,
}
