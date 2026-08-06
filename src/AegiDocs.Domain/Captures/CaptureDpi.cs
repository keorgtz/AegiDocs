namespace AegiDocs.Domain.Captures;

/// <summary>
/// Horizontal and vertical DPI values associated with a captured surface.
/// </summary>
public readonly record struct CaptureDpi
{
    /// <summary>
    /// Creates finite, positive DPI values.
    /// </summary>
    public CaptureDpi(double horizontal, double vertical)
    {
        Horizontal = Validate(horizontal, nameof(horizontal));
        Vertical = Validate(vertical, nameof(vertical));
    }

    /// <summary>
    /// Gets the positive horizontal DPI value.
    /// </summary>
    public double Horizontal { get; }

    /// <summary>
    /// Gets the positive vertical DPI value.
    /// </summary>
    public double Vertical { get; }

    private static double Validate(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A DPI value must be finite and positive.");
        }

        return value;
    }
}
