using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// A directional annotation between two distinct normalized points.
/// </summary>
public sealed record ArrowAnnotation : Annotation
{
    /// <summary>
    /// Creates a directional arrow.
    /// </summary>
    public ArrowAnnotation(AnnotationId id, int zIndex, NormalizedPoint start, NormalizedPoint end)
        : base(id, zIndex)
    {
        if (start == end)
        {
            throw new ArgumentException("An arrow start and end must be distinct.", nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the point from which the arrow originates.
    /// </summary>
    public NormalizedPoint Start { get; }

    /// <summary>
    /// Gets the point toward which the arrow points.
    /// </summary>
    public NormalizedPoint End { get; }
}
