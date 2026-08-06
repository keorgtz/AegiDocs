namespace AegiDocs.Domain.Geometry;

/// <summary>
/// A non-empty rectangle expressed relative to the bounds of a captured image or surface.
/// </summary>
public readonly record struct NormalizedRectangle
{
    /// <summary>
    /// Creates a normalized rectangle whose origin and extent remain inside its bounds.
    /// </summary>
    public NormalizedRectangle(double x, double y, double width, double height)
    {
        X = ValidateOrigin(x, nameof(x));
        Y = ValidateOrigin(y, nameof(y));
        Width = ValidateSize(width, nameof(width));
        Height = ValidateSize(height, nameof(height));

        if (X + Width > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The rectangle must not extend beyond the normalized horizontal bound.");
        }

        if (Y + Height > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The rectangle must not extend beyond the normalized vertical bound.");
        }
    }

    /// <summary>
    /// Gets the horizontal origin in the inclusive range from zero to one.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the vertical origin in the inclusive range from zero to one.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the positive normalized width.
    /// </summary>
    public double Width { get; }

    /// <summary>
    /// Gets the positive normalized height.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// Creates a normalized rectangle from physical pixel coordinates and bounds.
    /// </summary>
    public static NormalizedRectangle FromPixels(
        double x,
        double y,
        double width,
        double height,
        int pixelWidth,
        int pixelHeight)
    {
        NormalizedPoint.ValidateDimensions(pixelWidth, pixelHeight);
        return new NormalizedRectangle(x / pixelWidth, y / pixelHeight, width / pixelWidth, height / pixelHeight);
    }

    /// <summary>
    /// Converts this rectangle to physical pixel coordinates for the supplied bounds.
    /// </summary>
    public (double X, double Y, double Width, double Height) ToPixels(int pixelWidth, int pixelHeight)
    {
        NormalizedPoint.ValidateDimensions(pixelWidth, pixelHeight);
        return (X * pixelWidth, Y * pixelHeight, Width * pixelWidth, Height * pixelHeight);
    }

    private static double ValidateOrigin(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A normalized rectangle origin must be finite and within the inclusive range from zero to one.");
        }

        return value;
    }

    private static double ValidateSize(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A normalized rectangle size must be finite, positive, and no greater than one.");
        }

        return value;
    }
}
