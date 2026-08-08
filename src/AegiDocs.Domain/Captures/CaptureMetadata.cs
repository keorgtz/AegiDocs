using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Captures;

/// <summary>
/// Immutable, privacy-minimized technical metadata for a captured asset.
/// </summary>
/// <remarks>
/// This type deliberately excludes window titles, process names, process IDs,
/// paths, and user-identifying information. The monitor identifier is a required
/// technical display identifier supplied by the platform boundary. Native window
/// handles and other process-lifetime identifiers remain outside the domain.
/// </remarks>
public sealed record CaptureMetadata
{
    /// <summary>
    /// Creates capture metadata from validated, platform-independent value objects.
    /// </summary>
    public CaptureMetadata(
        AssetId assetId,
        PhysicalPixelSize pixelSize,
        CaptureDpi dpi,
        MonitorIdentifier monitorIdentifier,
        DateTimeOffset capturedAt)
    {
        if (assetId.Value == Guid.Empty)
        {
            throw new ArgumentException("An asset identifier is required.", nameof(assetId));
        }

        ArgumentNullException.ThrowIfNull(monitorIdentifier);

        AssetId = assetId;
        PixelSize = pixelSize;
        Dpi = dpi;
        MonitorIdentifier = monitorIdentifier;
        CapturedAt = NormalizeUtc(capturedAt);
    }

    /// <summary>
    /// Gets the identifier of the locally stored capture asset.
    /// </summary>
    public AssetId AssetId { get; }

    /// <summary>
    /// Gets the physical pixel dimensions of the captured surface.
    /// </summary>
    public PhysicalPixelSize PixelSize { get; }

    /// <summary>
    /// Gets the horizontal and vertical capture DPI values.
    /// </summary>
    public CaptureDpi Dpi { get; }

    /// <summary>
    /// Gets the required opaque technical identifier of the source monitor.
    /// </summary>
    public MonitorIdentifier MonitorIdentifier { get; }

    /// <summary>
    /// Gets the UTC instant at which the capture was acquired.
    /// </summary>
    public DateTimeOffset CapturedAt { get; }

    private static DateTimeOffset NormalizeUtc(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A capture timestamp is required.");
        }

        return value.ToUniversalTime();
    }
}
