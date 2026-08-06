using AegiDocs.Domain.Identifiers;

namespace AegiDocs.Domain.Annotations;

/// <summary>
/// Immutable visual instruction applied to a captured tutorial step.
/// </summary>
/// <remarks>
/// This model carries only semantic geometry and author-provided text. Renderers
/// choose their own styling, so no WPF, rendering, or concrete color types enter
/// the domain. Derived records are suitable for translation to versioned DTOs.
/// </remarks>
public abstract record Annotation
{
    /// <summary>
    /// Creates a validated annotation.
    /// </summary>
    protected Annotation(AnnotationId id, int zIndex)
    {
        if (id == default)
        {
            throw new ArgumentException("An annotation identifier is required.", nameof(id));
        }

        if (zIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zIndex), zIndex, "An annotation z-index cannot be negative.");
        }

        Id = id;
        ZIndex = zIndex;
    }

    /// <summary>
    /// Gets the stable identity used for editing and reordering.
    /// </summary>
    public AnnotationId Id { get; }

    /// <summary>
    /// Gets the non-negative draw order within a step.
    /// </summary>
    public int ZIndex { get; }
}
