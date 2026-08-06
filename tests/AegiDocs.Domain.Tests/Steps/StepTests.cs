using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Interactions;
using AegiDocs.Domain.Steps;
using Xunit;

namespace AegiDocs.Domain.Tests.Steps;

public sealed class StepTests
{
    [Fact]
    public void CreateValidInputNormalizesAndOrdersAnnotationsWithoutDroppingRedactions()
    {
        var redaction = new RedactionAnnotation(
            AnnotationId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            2,
            new NormalizedRectangle(0.1, 0.2, 0.3, 0.4),
            RedactionStyle.Solid);
        var marker = new NumberedMarkerAnnotation(
            AnnotationId.Create(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            1,
            new NormalizedPoint(0.5, 0.5),
            1);

        var step = CreateStep(title: "  Selecciona\tArchivo  ", description: "  Detalle del paso.  ", annotations: [redaction, marker]);

        Assert.Equal("Selecciona Archivo", step.Title);
        Assert.Equal("Detalle del paso.", step.Description);
        Assert.Equal([marker, redaction], step.Annotations);
        var retainedRedaction = Assert.IsType<RedactionAnnotation>(step.Annotations.Single(static annotation => annotation is RedactionAnnotation));
        Assert.Equal(redaction.Id, retainedRedaction.Id);
        Assert.Equal(redaction.Bounds, retainedRedaction.Bounds);
        Assert.Equal(RedactionStyle.Solid, retainedRedaction.Style);
    }

    [Fact]
    public void CreateMaterializesAnnotationsWithoutRetainingCallerCollectionMutability()
    {
        var annotations = new List<Annotation>
        {
            new NumberedMarkerAnnotation(AnnotationId.New(), 0, new NormalizedPoint(0.5, 0.5), 1),
        };

        var step = CreateStep(annotations: annotations);
        annotations.Clear();

        Assert.Single(step.Annotations);
        Assert.Throws<NotSupportedException>(() => ((IList<Annotation>)step.Annotations).Add(
            new NumberedMarkerAnnotation(AnnotationId.New(), 1, new NormalizedPoint(0.1, 0.1), 2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateNonPositiveOrderThrows(int order)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateStep(order: order));
    }

    [Fact]
    public void CreateMismatchedAssetIdThrows()
    {
        Assert.Throws<ArgumentException>(() => CreateStep(assetId: AssetId.New()));
    }

    [Fact]
    public void CreateCaptureBeforeInteractionThrows()
    {
        var capturedAt = new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        var interactionAt = capturedAt.AddSeconds(1);

        Assert.Throws<ArgumentException>(() => CreateStep(capturedAt: capturedAt, occurredAt: interactionAt));
    }

    [Fact]
    public void CreateDuplicateAnnotationIdentityOrZIndexThrows()
    {
        var id = AnnotationId.New();
        var first = new NumberedMarkerAnnotation(id, 0, new NormalizedPoint(0.1, 0.1), 1);
        var duplicateId = new RectangleAnnotation(id, 1, new NormalizedRectangle(0.2, 0.2, 0.1, 0.1));
        var duplicateZIndex = new NumberedMarkerAnnotation(AnnotationId.New(), 0, new NormalizedPoint(0.2, 0.2), 2);

        Assert.Throws<ArgumentException>(() => CreateStep(annotations: [first, duplicateId]));
        Assert.Throws<ArgumentException>(() => CreateStep(annotations: [first, duplicateZIndex]));
    }

    [Fact]
    public void CreateEmptyOrOversizedTextThrows()
    {
        Assert.Throws<ArgumentException>(() => CreateStep(title: " \t "));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateStep(title: new string('a', Step.MaximumTitleLength + 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateStep(description: new string('a', Step.MaximumDescriptionLength + 1)));
    }

    private static Step CreateStep(
        StepId? id = null,
        AssetId? assetId = null,
        int order = 1,
        string title = "Selecciona Archivo",
        string description = "",
        DateTimeOffset? capturedAt = null,
        DateTimeOffset? occurredAt = null,
        IEnumerable<Annotation>? annotations = null)
    {
        var captureAssetId = AssetId.New();
        var captureTime = capturedAt ?? new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        var interactionTime = occurredAt ?? captureTime;
        var capture = new CaptureMetadata(
            captureAssetId,
            new PhysicalPixelSize(1920, 1080),
            new CaptureDpi(96, 96),
            new MonitorIdentifier("DISPLAY1"),
            captureTime);
        var interaction = new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            new NormalizedPoint(0.5, 0.5),
            interactionTime,
            1);

        return new Step(
            id ?? StepId.New(),
            assetId ?? captureAssetId,
            order,
            title,
            description,
            capture,
            interaction,
            annotations);
    }
}
