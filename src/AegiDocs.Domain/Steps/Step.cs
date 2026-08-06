using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Interactions;

namespace AegiDocs.Domain.Steps;

/// <summary>
/// An immutable, captured instruction in a tutorial.
/// </summary>
/// <remarks>
/// A step keeps the capture asset, interaction, and annotations together so that
/// presentation, storage, and exporters cannot accidentally detach a privacy
/// redaction from the image to which it applies. Annotations are stored in their
/// deterministic draw order and are not rendered or transformed by the domain.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Step is the ubiquitous domain term and matches the published AegiDocs model.")]
public sealed record Step
{
    /// <summary>
    /// The maximum number of characters allowed in a normalized step title.
    /// </summary>
    public const int MaximumTitleLength = 200;

    /// <summary>
    /// The maximum number of characters allowed in a step description.
    /// </summary>
    public const int MaximumDescriptionLength = 4_000;

    /// <summary>
    /// Creates a validated, immutable tutorial step.
    /// </summary>
    public Step(
        StepId id,
        AssetId assetId,
        int order,
        string title,
        string description,
        CaptureMetadata capture,
        RecordedInteraction interaction,
        IEnumerable<Annotation>? annotations = null)
    {
        if (id == default)
        {
            throw new ArgumentException("A step identifier is required.", nameof(id));
        }

        if (assetId == default)
        {
            throw new ArgumentException("An asset identifier is required.", nameof(assetId));
        }

        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "A step order must be positive.");
        }

        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(interaction);

        if (capture.AssetId != assetId)
        {
            throw new ArgumentException("The capture asset identifier must match the step asset identifier.", nameof(capture));
        }

        if (capture.CapturedAt < interaction.OccurredAt)
        {
            throw new ArgumentException("A step capture cannot precede its recorded interaction.", nameof(capture));
        }

        Id = id;
        AssetId = assetId;
        Order = order;
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Capture = capture;
        Interaction = interaction;
        Annotations = CreateOrderedAnnotations(annotations);
    }

    /// <summary>
    /// Gets the stable identity of this step.
    /// </summary>
    public StepId Id { get; }

    /// <summary>
    /// Gets the identifier of the image asset associated with this step.
    /// </summary>
    public AssetId AssetId { get; }

    /// <summary>
    /// Gets the one-based tutorial order of this step.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the normalized, user-visible instruction title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the optional author-provided instruction detail.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the technical metadata for the capture asset.
    /// </summary>
    public CaptureMetadata Capture { get; }

    /// <summary>
    /// Gets the recorded interaction represented by this instruction.
    /// </summary>
    public RecordedInteraction Interaction { get; }

    /// <summary>
    /// Gets annotations in ascending z-index order, including all privacy redactions.
    /// </summary>
    public IReadOnlyList<Annotation> Annotations { get; }

    private static string NormalizeTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var normalizedTitle = string.Join(' ', title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalizedTitle.Length == 0)
        {
            throw new ArgumentException("A step title is required.", nameof(title));
        }

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new ArgumentOutOfRangeException(nameof(title), title, $"A step title cannot exceed {MaximumTitleLength} characters.");
        }

        return normalizedTitle;
    }

    private static string NormalizeDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new ArgumentOutOfRangeException(nameof(description), description, $"A step description cannot exceed {MaximumDescriptionLength} characters.");
        }

        return normalizedDescription;
    }

    private static ReadOnlyCollection<Annotation> CreateOrderedAnnotations(IEnumerable<Annotation>? annotations)
    {
        var items = annotations?.ToArray() ?? [];
        if (items.Any(static annotation => annotation is null))
        {
            throw new ArgumentException("A step annotation collection cannot contain null values.", nameof(annotations));
        }

        var duplicateId = items.GroupBy(static annotation => annotation.Id).FirstOrDefault(static group => group.Count() > 1);
        if (duplicateId is not null)
        {
            throw new ArgumentException("A step cannot contain annotations with duplicate identifiers.", nameof(annotations));
        }

        var duplicateZIndex = items.GroupBy(static annotation => annotation.ZIndex).FirstOrDefault(static group => group.Count() > 1);
        if (duplicateZIndex is not null)
        {
            throw new ArgumentException("A step cannot contain annotations with duplicate z-index values.", nameof(annotations));
        }

        return new ReadOnlyCollection<Annotation>(items.OrderBy(static annotation => annotation.ZIndex).ToArray());
    }
}
