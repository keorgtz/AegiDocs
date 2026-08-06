namespace AegiDocs.Domain.Interactions;

/// <summary>
/// Describes an interaction intentionally retained by the MVP recorder.
/// </summary>
/// <remarks>
/// The MVP retains mouse clicks only. Keyboard input, typed text, clipboard
/// contents, control metadata, process metadata, and window metadata are
/// intentionally outside this domain contract.
/// </remarks>
public enum InteractionType
{
    /// <summary>
    /// A click performed with a mouse button.
    /// </summary>
    MouseClick = 1,
}
