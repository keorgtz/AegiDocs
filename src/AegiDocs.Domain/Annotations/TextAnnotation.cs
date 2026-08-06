using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// Explicit author-provided explanatory text constrained to normalized bounds.
/// </summary>
public sealed record TextAnnotation : Annotation
{
    /// <summary>
    /// Creates a text annotation.
    /// </summary>
    public TextAnnotation(AnnotationId id, int zIndex, string content, NormalizedRectangle bounds)
        : base(id, zIndex)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Annotation text cannot be empty or whitespace.", nameof(content));
        }

        Content = content;
        Bounds = bounds;
    }

    /// <summary>
    /// Gets the text explicitly supplied by the author.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the normalized bounds in which a renderer lays out the text.
    /// </summary>
    public NormalizedRectangle Bounds { get; }
}
