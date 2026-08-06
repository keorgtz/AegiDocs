using AegiDocs.Domain.Geometry;

namespace AegiDocs.Domain.Interactions;

/// <summary>
/// Immutable, privacy-minimized interaction retained for a tutorial step.
/// </summary>
/// <remarks>
/// The recorder stores only a click's kind, button, normalized position, UTC
/// instant, and positive sequence. It deliberately does not retain keyboard
/// input, text, clipboard content, control names, process data, window data,
/// or any other user-identifying information.
/// </remarks>
public sealed record RecordedInteraction
{
    /// <summary>
    /// Creates a validated recorded mouse-click interaction.
    /// </summary>
    public RecordedInteraction(
        InteractionType type,
        MouseButton button,
        NormalizedPoint position,
        DateTimeOffset occurredAt,
        long sequence)
    {
        if (type != InteractionType.MouseClick)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Only mouse-click interactions are supported.");
        }

        if (!Enum.IsDefined(button))
        {
            throw new ArgumentOutOfRangeException(nameof(button), button, "A supported mouse button is required.");
        }

        if (occurredAt == default)
        {
            throw new ArgumentOutOfRangeException(nameof(occurredAt), occurredAt, "An interaction timestamp is required.");
        }

        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "An interaction sequence must be positive.");
        }

        Type = type;
        Button = button;
        Position = position;
        OccurredAt = occurredAt.ToUniversalTime();
        Sequence = sequence;
    }

    /// <summary>
    /// Gets the retained interaction kind.
    /// </summary>
    public InteractionType Type { get; }

    /// <summary>
    /// Gets the button used for the click.
    /// </summary>
    public MouseButton Button { get; }

    /// <summary>
    /// Gets the click position relative to the captured surface.
    /// </summary>
    public NormalizedPoint Position { get; }

    /// <summary>
    /// Gets the UTC instant at which the click occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Gets the one-based order of the interaction in its recording session.
    /// </summary>
    public long Sequence { get; }
}
