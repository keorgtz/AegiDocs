using System.Globalization;

namespace AegiDocs.Domain.Projects;

/// <summary>
/// Immutable descriptive metadata for a document project.
/// </summary>
/// <remarks>
/// Project preferences are intentionally deferred. They belong to explicit product
/// behavior contracts (for example, recorder defaults) rather than durable project
/// metadata, and adding them now would prematurely couple the domain to future UI.
/// </remarks>
public sealed record ProjectMetadata
{
    /// <summary>
    /// The maximum number of characters allowed in a normalized project name.
    /// </summary>
    public const int MaximumNameLength = 200;

    /// <summary>
    /// Creates immutable project metadata.
    /// </summary>
    public ProjectMetadata(
        string name,
        string languageTag,
        DateTimeOffset createdAt,
        DateTimeOffset modifiedAt,
        int schemaVersion)
    {
        Name = NormalizeName(name);
        LanguageTag = NormalizeLanguageTag(languageTag);
        CreatedAt = NormalizeUtc(createdAt, nameof(createdAt));
        ModifiedAt = NormalizeUtc(modifiedAt, nameof(modifiedAt));

        if (ModifiedAt < CreatedAt)
        {
            throw new ArgumentException("The modified timestamp cannot precede the creation timestamp.", nameof(modifiedAt));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "The schema version must be positive.");
        }

        SchemaVersion = schemaVersion;
    }

    /// <summary>
    /// Gets the normalized, user-visible project name.
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// Gets the normalized BCP-47 language tag used for user-authored content.
    /// </summary>
    public string LanguageTag { get; private init; }

    /// <summary>
    /// Gets the UTC instant at which the project was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Gets the UTC instant at which the project was last modified.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; private init; }

    /// <summary>
    /// Gets the positive schema version used to persist this project.
    /// </summary>
    public int SchemaVersion { get; private init; }

    private static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var normalizedName = string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalizedName.Length == 0)
        {
            throw new ArgumentException("A project name is required.", nameof(name));
        }

        if (normalizedName.Length > MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name), name, $"A project name cannot exceed {MaximumNameLength} characters.");
        }

        return normalizedName;
    }

    private static string NormalizeLanguageTag(string languageTag)
    {
        ArgumentNullException.ThrowIfNull(languageTag);

        var normalizedTag = languageTag.Trim();
        if (normalizedTag.Length == 0)
        {
            throw new ArgumentException("A BCP-47 language tag is required.", nameof(languageTag));
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalizedTag);
            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException("The invariant culture is not a valid project language.", nameof(languageTag));
            }

            return culture.Name;
        }
        catch (CultureNotFoundException exception)
        {
            throw new ArgumentException("The language tag must identify a valid BCP-47 culture.", nameof(languageTag), exception);
        }
    }

    private static DateTimeOffset NormalizeUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A timestamp is required.");
        }

        return value.ToUniversalTime();
    }
}
