using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// A rectangular emphasis region.
/// </summary>
public sealed record RectangleAnnotation : Annotation
{
    /// <summary>
    /// Creates a rectangular annotation.
    /// </summary>
    public RectangleAnnotation(AnnotationId id, int zIndex, NormalizedRectangle bounds)
        : base(id, zIndex)
    {
        Bounds = bounds;
    }

    /// <summary>
    /// Gets the emphasized normalized bounds.
    /// </summary>
    public NormalizedRectangle Bounds { get; }
}
