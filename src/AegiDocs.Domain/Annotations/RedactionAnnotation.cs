using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// A privacy redaction that must be flattened by publication renderers.
/// </summary>
/// <remarks>
/// Publication renderers must apply an opaque solid mask and must not preserve
/// source pixels inside <see cref="Bounds"/>. Pixelation is deliberately not a
/// supported privacy redaction because it does not guarantee obscuration.
/// </remarks>
public sealed record RedactionAnnotation : Annotation
{
    /// <summary>
    /// Creates a privacy redaction.
    /// </summary>
    public RedactionAnnotation(AnnotationId id, int zIndex, NormalizedRectangle bounds, RedactionStyle style)
        : base(id, zIndex)
    {
        if (!Enum.IsDefined(style))
        {
            throw new ArgumentOutOfRangeException(nameof(style), style, "A supported redaction style is required.");
        }

        Bounds = bounds;
        Style = style;
    }

    /// <summary>
    /// Gets the normalized region that must be redacted.
    /// </summary>
    public NormalizedRectangle Bounds { get; }

    /// <summary>
    /// Gets the semantic redaction style for a renderer.
    /// </summary>
    public RedactionStyle Style { get; }
}
