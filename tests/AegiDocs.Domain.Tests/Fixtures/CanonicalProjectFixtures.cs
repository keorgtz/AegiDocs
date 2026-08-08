using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Interactions;
using AegiDocs.Domain.Projects;
using AegiDocs.Domain.Serialization;
using AegiDocs.Domain.Steps;
using AegiDocs.Domain.Tutorials;

namespace AegiDocs.Domain.Tests.Fixtures;

/// <summary>
/// Synthetic, deterministic domain fixtures for future Storage, Editor, and Export tests.
/// </summary>
/// <remarks>
/// All identities, timestamps, display identifiers, and text are fixed fictional values.
/// These builders contain no file paths, images, machine state, I/O, or user-derived data.
/// </remarks>
public static class CanonicalProjectFixtures
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates an empty project using the current schema version.
    /// </summary>
    public static DocumentProject CreateEmptyProject() => new(
        ProjectId.Create(CreateGuid(1)),
        CreateMetadata("Empty project"));

    /// <summary>
    /// Creates one tutorial with one captured step and no annotations.
    /// </summary>
    public static Tutorial CreateSimpleTutorial() => new(
        TutorialId.Create(CreateGuid(101)),
        1,
        "Create a sample document",
        "Open the synthetic workspace and select New.",
        "Test author",
        [CreateStep(1, 1)]);

    /// <summary>
    /// Creates a project with every supported annotation, including a solid privacy redaction.
    /// </summary>
    public static DocumentProject CreateCompleteAnnotationsProject()
    {
        var annotations = new Annotation[]
        {
            new NumberedMarkerAnnotation(AnnotationId.Create(CreateGuid(2_001)), 0, new NormalizedPoint(0.15, 0.15), 1),
            new RectangleAnnotation(AnnotationId.Create(CreateGuid(2_002)), 1, new NormalizedRectangle(0.2, 0.2, 0.25, 0.2)),
            new ArrowAnnotation(AnnotationId.Create(CreateGuid(2_003)), 2, new NormalizedPoint(0.1, 0.8), new NormalizedPoint(0.4, 0.5)),
            new TextAnnotation(AnnotationId.Create(CreateGuid(2_004)), 3, "Synthetic instruction", new NormalizedRectangle(0.5, 0.1, 0.3, 0.15)),
            new RedactionAnnotation(AnnotationId.Create(CreateGuid(2_005)), 4, new NormalizedRectangle(0.55, 0.55, 0.2, 0.15), RedactionStyle.Solid),
        };
        var step = CreateStep(2, 1, annotations);
        var tutorial = new Tutorial(
            TutorialId.Create(CreateGuid(102)),
            1,
            "Annotate a synthetic capture",
            "All visual annotation kinds are represented.",
            "Test author",
            [step]);

        return new DocumentProject(ProjectId.Create(CreateGuid(2)), CreateMetadata("Complete annotations project"), [tutorial]);
    }

    /// <summary>
    /// Creates a deterministic project large enough to exercise collection consumers.
    /// </summary>
    public static DocumentProject CreateLargeProject()
    {
        const int tutorialCount = 16;
        const int stepsPerTutorial = 24;
        var tutorials = Enumerable.Range(1, tutorialCount)
            .Select(tutorialOrder => new Tutorial(
                TutorialId.Create(CreateGuid(3_000 + tutorialOrder)),
                tutorialOrder,
                $"Synthetic tutorial {tutorialOrder}",
                "Deterministic fixture content.",
                "Test author",
                Enumerable.Range(1, stepsPerTutorial)
                    .Select(stepOrder => CreateStep((tutorialOrder * 100) + stepOrder, stepOrder))
                    .ToArray()))
            .ToArray();

        return new DocumentProject(ProjectId.Create(CreateGuid(3)), CreateMetadata("Large synthetic project"), tutorials);
    }

    private static ProjectMetadata CreateMetadata(string name) => new(
        name,
        "en-US",
        Timestamp,
        Timestamp.AddMinutes(1),
        ProjectSchema.CurrentVersion);

    private static Step CreateStep(int identity, int order, IEnumerable<Annotation>? annotations = null)
    {
        var assetId = AssetId.Create(CreateGuid(10_000 + identity));
        var eventTime = Timestamp.AddSeconds(identity);
        var capture = new CaptureMetadata(
            assetId,
            new PhysicalPixelSize(1280, 720),
            new CaptureDpi(96, 96),
            new MonitorIdentifier("SYNTHETIC-DISPLAY-1"),
            eventTime);
        var interaction = new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            new NormalizedPoint(0.5, 0.5),
            eventTime,
            identity);

        return new Step(
            StepId.Create(CreateGuid(20_000 + identity)),
            assetId,
            order,
            $"Synthetic step {identity}",
            "Synthetic, non-sensitive detail.",
            capture,
            interaction,
            annotations);
    }

    private static Guid CreateGuid(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
}
