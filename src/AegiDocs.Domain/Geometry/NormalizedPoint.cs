namespace AegiDocs.Domain.Geometry;

/// <summary>
/// A point expressed relative to the bounds of a captured image or surface.
/// </summary>
/// <remarks>
/// Both coordinates are finite values in the inclusive range from zero to one.
/// Pixel conversion deliberately uses only physical dimensions; DPI belongs to
/// the platform boundary and is not represented by the domain.
/// </remarks>
public readonly record struct NormalizedPoint
{
    /// <summary>
    /// Creates a normalized point.
    /// </summary>
    public NormalizedPoint(double x, double y)
    {
        X = ValidateCoordinate(x, nameof(x));
        Y = ValidateCoordinate(y, nameof(y));
    }

    /// <summary>
    /// Gets the horizontal coordinate in the inclusive range from zero to one.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the vertical coordinate in the inclusive range from zero to one.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a normalized point from physical pixel coordinates and bounds.
    /// </summary>
    public static NormalizedPoint FromPixels(double x, double y, int pixelWidth, int pixelHeight)
    {
        ValidateDimensions(pixelWidth, pixelHeight);
        return new NormalizedPoint(x / pixelWidth, y / pixelHeight);
    }

    /// <summary>
    /// Converts this point to physical pixel coordinates for the supplied bounds.
    /// </summary>
    public (double X, double Y) ToPixels(int pixelWidth, int pixelHeight)
    {
        ValidateDimensions(pixelWidth, pixelHeight);
        return (X * pixelWidth, Y * pixelHeight);
    }

    internal static void ValidateDimensions(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), pixelWidth, "The pixel width must be positive.");
        }

        if (pixelHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), pixelHeight, "The pixel height must be positive.");
        }
    }

    private static double ValidateCoordinate(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A normalized coordinate must be finite and within the inclusive range from zero to one.");
        }

        return value;
    }
}
