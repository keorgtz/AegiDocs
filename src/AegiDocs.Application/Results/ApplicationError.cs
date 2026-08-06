namespace AegiDocs.Application;

/// <summary>
/// Describes an expected application failure that is safe to present to a user.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UserMessage" /> must be safe for the UI. Do not include personal data,
/// file paths, captured content, credentials, or exception messages in it.
/// </para>
/// <para>
/// <see cref="TechnicalDetail" /> is optional diagnostic context for local handling. It
/// must also be free of personal or sensitive data and is not an <see cref="Exception" />.
/// </para>
/// </remarks>
public sealed record ApplicationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationError" /> class.
    /// </summary>
    public ApplicationError(
        ApplicationErrorCode code,
        string userMessage,
        string? technicalDetail = null)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(nameof(code), code, "The error code must be defined.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        Code = code;
        UserMessage = userMessage;
        TechnicalDetail = technicalDetail;
    }

    /// <summary>
    /// Gets the semantic category of the failure.
    /// </summary>
    public ApplicationErrorCode Code { get; }

    /// <summary>
    /// Gets the safe message that may be shown to a user.
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Gets optional, non-sensitive diagnostic context.
    /// </summary>
    public string? TechnicalDetail { get; }
}
