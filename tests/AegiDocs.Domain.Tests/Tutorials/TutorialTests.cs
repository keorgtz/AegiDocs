using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Interactions;
using AegiDocs.Domain.Steps;
using AegiDocs.Domain.Tutorials;
using Xunit;

namespace AegiDocs.Domain.Tests.Tutorials;

public sealed class TutorialTests
{
    [Fact]
    public void ConstructorNormalizesDetailsAndOrdersMaterializedSteps()
    {
        var laterStep = CreateStep(order: 2, title: "Segundo paso");
        var firstStep = CreateStep(order: 1, title: "Primer paso");

        var tutorial = CreateTutorial(
            title: "  Registrar\tun documento  ",
            description: "  Crea un tutorial.  ",
            audience: "  Equipo\nde soporte  ",
            steps: [laterStep, firstStep]);

        Assert.Equal("Registrar un documento", tutorial.Title);
        Assert.Equal("Crea un tutorial.", tutorial.Description);
        Assert.Equal("Equipo\nde soporte", tutorial.Audience);
        Assert.Equal([firstStep, laterStep], tutorial.Steps);
    }

    [Fact]
    public void ConstructorDoesNotRetainCallerCollectionMutability()
    {
        var steps = new List<Step> { CreateStep(order: 1) };
        var tutorial = CreateTutorial(steps: steps);

        steps.Clear();

        Assert.Single(tutorial.Steps);
        Assert.Throws<NotSupportedException>(() => ((IList<Step>)tutorial.Steps).Add(CreateStep(order: 2)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsNonPositiveTutorialOrder(int order)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTutorial(order: order));
    }

    [Fact]
    public void ConstructorRejectsEmptyTutorialIdentity()
    {
        Assert.Throws<ArgumentException>(() => new Tutorial(default, 1, "Registrar un documento", "", ""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void ConstructorRejectsMissingTitle(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateTutorial(title: title!));
    }

    [Fact]
    public void ConstructorRejectsOversizedDetailsAndNullOptionalContent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTutorial(title: new string('a', Tutorial.MaximumTitleLength + 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTutorial(description: new string('a', Tutorial.MaximumDescriptionLength + 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTutorial(audience: new string('a', Tutorial.MaximumAudienceLength + 1)));
        Assert.Throws<ArgumentNullException>(() => CreateTutorial(description: null!));
        Assert.Throws<ArgumentNullException>(() => CreateTutorial(audience: null!));
    }

    [Fact]
    public void ConstructorRejectsDuplicateStepIdentityAndOrder()
    {
        var first = CreateStep(id: StepId.New(), order: 1);
        var duplicateId = CreateStep(id: first.Id, order: 2);
        var duplicateOrder = CreateStep(order: 1);

        Assert.Throws<ArgumentException>(() => CreateTutorial(steps: [first, duplicateId]));
        Assert.Throws<ArgumentException>(() => CreateTutorial(steps: [first, duplicateOrder]));
    }

    [Fact]
    public void WithDetailsReturnsNewTutorialAndPreservesIdentityOrderAndSteps()
    {
        var source = CreateTutorial(steps: [CreateStep(order: 1)]);

        var updated = source.WithDetails("  Nuevo\ttítulo ", "  Nuevo detalle. ", "  Personal nuevo ");

        Assert.NotSame(source, updated);
        Assert.Equal(source.Id, updated.Id);
        Assert.Equal(source.Order, updated.Order);
        Assert.Equal(source.Steps, updated.Steps);
        Assert.Equal("Nuevo título", updated.Title);
        Assert.Equal("Nuevo detalle.", updated.Description);
        Assert.Equal("Personal nuevo", updated.Audience);
    }

    [Fact]
    public void WithStepsReturnsNewTutorialWithDeterministicStepOrder()
    {
        var source = CreateTutorial(steps: [CreateStep(order: 1)]);
        var first = CreateStep(order: 1);
        var second = CreateStep(order: 2);

        var updated = source.WithSteps([second, first]);

        Assert.NotSame(source, updated);
        Assert.Equal([first, second], updated.Steps);
        Assert.Equal(source.Title, updated.Title);
    }

    [Fact]
    public void MoveStepToPositionReturnsNewTutorialAndRenumbersSteps()
    {
        var first = CreateStep(order: 10, title: "Primero");
        var second = CreateStep(order: 20, title: "Segundo");
        var third = CreateStep(order: 30, title: "Tercero");
        var source = CreateTutorial(steps: [first, second, third]);

        var moved = source.MoveStepToPosition(third.Id, 1);

        Assert.NotSame(source, moved);
        Assert.Equal([third.Id, first.Id, second.Id], moved.Steps.Select(static step => step.Id));
        Assert.Equal([1, 2, 3], moved.Steps.Select(static step => step.Order));
        Assert.Equal([10, 20, 30], source.Steps.Select(static step => step.Order));
    }

    [Fact]
    public void MoveStepToPositionValidatesIdentityPositionAndMembership()
    {
        var source = CreateTutorial(steps: [CreateStep(order: 1)]);

        Assert.Throws<ArgumentException>(() => source.MoveStepToPosition(default, 1));
        Assert.Throws<ArgumentException>(() => source.MoveStepToPosition(StepId.New(), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.MoveStepToPosition(source.Steps[0].Id, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.MoveStepToPosition(source.Steps[0].Id, 2));
    }

    private static Tutorial CreateTutorial(
        TutorialId? id = null,
        int order = 1,
        string title = "Registrar un documento",
        string description = "",
        string audience = "",
        IEnumerable<Step>? steps = null) => new(
        id ?? TutorialId.New(),
        order,
        title,
        description,
        audience,
        steps);

    private static Step CreateStep(StepId? id = null, int order = 1, string title = "Selecciona Archivo")
    {
        var assetId = AssetId.New();
        var occurredAt = new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        var capture = new CaptureMetadata(
            assetId,
            new PhysicalPixelSize(1920, 1080),
            new CaptureDpi(96, 96),
            new MonitorIdentifier("DISPLAY1"),
            occurredAt);
        var interaction = new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            new NormalizedPoint(0.5, 0.5),
            occurredAt,
            1);

        return new Step(id ?? StepId.New(), assetId, order, title, "", capture, interaction);
    }
}
