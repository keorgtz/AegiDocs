namespace AegiDocs.Domain.Interactions;

/// <summary>
/// Identifies the physical mouse button used for a recorded click.
/// </summary>
public enum MouseButton
{
    /// <summary>The primary mouse button.</summary>
    Left = 1,

    /// <summary>The secondary mouse button.</summary>
    Right = 2,

    /// <summary>The middle mouse button.</summary>
    Middle = 3,

    /// <summary>The first auxiliary mouse button.</summary>
    XButton1 = 4,

    /// <summary>The second auxiliary mouse button.</summary>
    XButton2 = 5,
}
