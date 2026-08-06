using System.Collections.ObjectModel;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Steps;

namespace AegiDocs.Domain.Tutorials;

/// <summary>
/// An immutable, ordered collection of instructions that teaches one workflow.
/// </summary>
/// <remarks>
/// The tutorial owns the deterministic ordering of its steps. Editing methods
/// return a new aggregate so callers cannot expose mutable state while building
/// an editor, storage DTO, or export projection.
/// </remarks>
public sealed record Tutorial
{
    /// <summary>
    /// The maximum number of characters allowed in a normalized tutorial title.
    /// </summary>
    public const int MaximumTitleLength = 200;

    /// <summary>
    /// The maximum number of characters allowed in a tutorial description.
    /// </summary>
    public const int MaximumDescriptionLength = 4_000;

    /// <summary>
    /// The maximum number of characters allowed in an optional audience description.
    /// </summary>
    public const int MaximumAudienceLength = 200;

    /// <summary>
    /// Creates a validated, immutable tutorial.
    /// </summary>
    public Tutorial(
        TutorialId id,
        int order,
        string title,
        string description,
        string audience,
        IEnumerable<Step>? steps = null)
    {
        if (id == default)
        {
            throw new ArgumentException("A tutorial identifier is required.", nameof(id));
        }

        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "A tutorial order must be positive.");
        }

        Id = id;
        Order = order;
        Title = NormalizeRequiredText(title, nameof(title), MaximumTitleLength, "A tutorial title is required.");
        Description = NormalizeOptionalText(description, nameof(description), MaximumDescriptionLength);
        Audience = NormalizeOptionalText(audience, nameof(audience), MaximumAudienceLength);
        Steps = CreateOrderedSteps(steps);
    }

    /// <summary>
    /// Gets the stable identity of this tutorial.
    /// </summary>
    public TutorialId Id { get; }

    /// <summary>
    /// Gets the one-based project order of this tutorial.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the normalized, user-visible tutorial title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the optional author-provided tutorial description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the optional intended audience supplied by the author.
    /// </summary>
    public string Audience { get; }

    /// <summary>
    /// Gets steps in ascending, unique one-based order.
    /// </summary>
    public IReadOnlyList<Step> Steps { get; }

    /// <summary>
    /// Returns a new tutorial with normalized descriptive content.
    /// </summary>
    public Tutorial WithDetails(string title, string description, string audience) => new(Id, Order, title, description, audience, Steps);

    /// <summary>
    /// Returns a new tutorial with a validated, deterministically ordered step collection.
    /// </summary>
    public Tutorial WithSteps(IEnumerable<Step>? steps) => new(Id, Order, Title, Description, Audience, steps);

    /// <summary>
    /// Returns a tutorial with one step moved to the supplied one-based position.
    /// </summary>
    /// <remarks>
    /// The returned tutorial renumbers every step consecutively, which makes the
    /// new order explicit and avoids ambiguous gaps in a persisted edit operation.
    /// </remarks>
    public Tutorial MoveStepToPosition(StepId stepId, int position)
    {
        if (stepId == default)
        {
            throw new ArgumentException("A step identifier is required.", nameof(stepId));
        }

        if (position <= 0 || position > Steps.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The destination position must identify an existing one-based step position.");
        }

        var orderedSteps = Steps.ToList();
        var sourceIndex = orderedSteps.FindIndex(step => step.Id == stepId);
        if (sourceIndex < 0)
        {
            throw new ArgumentException("The tutorial does not contain the requested step.", nameof(stepId));
        }

        var destinationIndex = position - 1;
        if (sourceIndex == destinationIndex)
        {
            return this;
        }

        var step = orderedSteps[sourceIndex];
        orderedSteps.RemoveAt(sourceIndex);
        orderedSteps.Insert(destinationIndex, step);

        var reorderedSteps = orderedSteps
            .Select((currentStep, index) => CopyWithOrder(currentStep, index + 1))
            .ToArray();

        return new Tutorial(Id, Order, Title, Description, Audience, reorderedSteps);
    }

    private static Step CopyWithOrder(Step step, int order) => new(
        step.Id,
        step.AssetId,
        order,
        step.Title,
        step.Description,
        step.Capture,
        step.Interaction,
        step.Annotations);

    private static ReadOnlyCollection<Step> CreateOrderedSteps(IEnumerable<Step>? steps)
    {
        var items = steps?.ToArray() ?? [];
        if (items.Any(static step => step is null))
        {
            throw new ArgumentException("A tutorial step collection cannot contain null values.", nameof(steps));
        }

        if (items.GroupBy(static step => step.Id).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("A tutorial cannot contain steps with duplicate identifiers.", nameof(steps));
        }

        if (items.GroupBy(static step => step.Order).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("A tutorial cannot contain steps with duplicate orders.", nameof(steps));
        }

        return new ReadOnlyCollection<Step>(items.OrderBy(static step => step.Order).ToArray());
    }

    private static string NormalizeRequiredText(string value, string parameterName, int maximumLength, string requiredMessage)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalizedValue = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException(requiredMessage, parameterName);
        }

        return ValidateMaximumLength(normalizedValue, parameterName, maximumLength);
    }

    private static string NormalizeOptionalText(string value, string parameterName, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ValidateMaximumLength(value.Trim(), parameterName, maximumLength);
    }

    private static string ValidateMaximumLength(string value, string parameterName, int maximumLength) => value.Length > maximumLength
        ? throw new ArgumentOutOfRangeException(parameterName, value, $"The value cannot exceed {maximumLength} characters.")
        : value;
}
