using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// A numbered marker placed at a normalized point.
/// </summary>
public sealed record NumberedMarkerAnnotation : Annotation
{
    /// <summary>
    /// Creates a numbered marker.
    /// </summary>
    public NumberedMarkerAnnotation(AnnotationId id, int zIndex, NormalizedPoint position, int number)
        : base(id, zIndex)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "A marker number must be positive.");
        }

        Position = position;
        Number = number;
    }

    /// <summary>
    /// Gets the marker position relative to the capture.
    /// </summary>
    public NormalizedPoint Position { get; }

    /// <summary>
    /// Gets the positive number displayed by a renderer.
    /// </summary>
    public int Number { get; }
}
