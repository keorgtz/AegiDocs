namespace AegiDocs.Domain.Annotations;

/// <summary>
/// Defines the renderer-independent appearance of a privacy redaction.
/// </summary>
public enum RedactionStyle
{
    /// <summary>
    /// Fully obscures the selected bounds with an opaque mask.
    /// </summary>
    Solid = 0,
}
