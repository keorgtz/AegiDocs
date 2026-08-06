namespace AegiDocs.Domain.Identifiers;

/// <summary>
/// Identifies a locally stored asset within a document project.
/// </summary>
public readonly record struct AssetId
{
    private AssetId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the GUID value used for durable storage boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new asset identifier.
    /// </summary>
    public static AssetId New() => new(Guid.NewGuid());

    /// <summary>
    /// Reconstitutes an asset identifier from a non-empty GUID.
    /// </summary>
    public static AssetId Create(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("An asset identifier cannot be empty.", nameof(value))
        : new AssetId(value);

    /// <summary>
    /// Parses an asset identifier persisted as a GUID.
    /// </summary>
    public static AssetId Parse(string value) => Create(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a non-empty asset identifier persisted as a GUID.
    /// </summary>
    public static bool TryParse(string? value, out AssetId assetId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            assetId = new AssetId(guid);
            return true;
        }

        assetId = default;
        return false;
    }
}
