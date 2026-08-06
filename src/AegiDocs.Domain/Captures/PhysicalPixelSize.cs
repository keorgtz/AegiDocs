namespace AegiDocs.Domain.Captures;

/// <summary>
/// Physical pixel dimensions of a captured image or surface.
/// </summary>
public readonly record struct PhysicalPixelSize
{
    /// <summary>
    /// Creates a non-empty physical pixel size.
    /// </summary>
    public PhysicalPixelSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The physical pixel width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The physical pixel height must be positive.");
        }

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the positive physical pixel width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the positive physical pixel height.
    /// </summary>
    public int Height { get; }
}
