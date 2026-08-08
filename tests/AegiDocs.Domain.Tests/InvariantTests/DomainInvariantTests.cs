using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Interactions;
using AegiDocs.Domain.Operations;
using AegiDocs.Domain.Projects;
using AegiDocs.Domain.Steps;
using AegiDocs.Domain.Tutorials;
using Xunit;

namespace AegiDocs.Domain.Tests.InvariantTests;

public sealed class DomainInvariantTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyAggregatesAndBoundaryIdentifiersPreserveTheirInvariants()
    {
        var metadata = CreateMetadata();
        var project = new DocumentProject(ProjectId.New(), metadata);
        var tutorial = new Tutorial(TutorialId.New(), 1, "Título", "", "");

        Assert.Empty(project.Tutorials);
        Assert.Empty(tutorial.Steps);
        Assert.Throws<ArgumentException>(() => new DocumentProject(default, metadata));
        Assert.Throws<ArgumentException>(() => new Tutorial(default, 1, "Título", "", ""));
        Assert.Throws<ArgumentException>(() => CreateStep(default, AssetId.New(), 1));
        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Move(
            [new TestItem(Guid.Parse("00000000-0000-0000-0000-000000000001"))],
            Guid.Empty,
            0,
            static item => item.Id));
    }

    [Fact]
    public void TutorialMoveStepToPositionMaintainsStepIdentityAndConsecutiveOrderAcrossDeterministicRandomMoves()
    {
        var steps = Enumerable.Range(1, 16)
            .Select(order => CreateStep(StepId.New(), AssetId.New(), order))
            .ToArray();
        var tutorial = new Tutorial(TutorialId.New(), 1, "Flujo", "", "", steps);
        var expectedIds = tutorial.Steps.Select(static step => step.Id).OrderBy(static id => id.Value).ToArray();
        var random = new Random(684_331);

        for (var iteration = 0; iteration < 160; iteration++)
        {
            var source = tutorial.Steps[random.Next(tutorial.Steps.Count)];
            tutorial = tutorial.MoveStepToPosition(source.Id, random.Next(1, tutorial.Steps.Count + 1));

            Assert.Equal(16, tutorial.Steps.Count);
            Assert.Equal(Enumerable.Range(1, 16), tutorial.Steps.Select(static step => step.Order));
            Assert.Equal(expectedIds, tutorial.Steps.Select(static step => step.Id).OrderBy(static id => id.Value));
            Assert.All(tutorial.Steps, static step => Assert.Equal(step.AssetId, step.Capture.AssetId));
        }
    }

    [Fact]
    public void DocumentProjectMaintainsTutorialUniquenessAndOrderingAfterFunctionalUpdates()
    {
        var tutorials = Enumerable.Range(1, 12)
            .Select(order => new Tutorial(TutorialId.New(), order, $"Tutorial {order}", "", ""))
            .Reverse()
            .ToArray();
        var project = new DocumentProject(ProjectId.New(), CreateMetadata(), tutorials);

        Assert.Equal(Enumerable.Range(1, 12), project.Tutorials.Select(static tutorial => tutorial.Order));
        Assert.Equal(12, project.Tutorials.Select(static tutorial => tutorial.Id).Distinct().Count());

        var replacement = new Tutorial(project.Tutorials[4].Id, 5, "Actualizado", "", "");
        var updated = project.ReplaceTutorial(replacement).WithMetadata(CreateMetadata("Manual actualizado"));
        var removed = updated.RemoveTutorial(project.Tutorials[9].Id);

        Assert.Equal("Manual actualizado", updated.Metadata.Name);
        Assert.Equal(replacement, updated.Tutorials.Single(static tutorial => tutorial.Order == 5));
        Assert.Equal(11, removed.Tutorials.Count);
        Assert.Equal(11, removed.Tutorials.Select(static tutorial => tutorial.Id).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 9).Concat(Enumerable.Range(11, 2)), removed.Tutorials.Select(static tutorial => tutorial.Order));
    }

    [Fact]
    public void OrderedCollectionOperationsMaintainIdentityAndImmutabilityAcrossDeterministicRandomOperations()
    {
        var nextIdentity = 9;
        IReadOnlyList<TestItem> items = Enumerable.Range(1, 8).Select(CreateItem).ToArray();
        var random = new Random(9_417_052);

        for (var iteration = 0; iteration < 200; iteration++)
        {
            var operation = random.Next(4);
            if (operation == 0)
            {
                var item = items[random.Next(items.Count)];
                items = OrderedCollectionOperations.Move(items, item.Id, random.Next(items.Count), static current => current.Id);
            }
            else if (operation == 1)
            {
                var source = items[random.Next(items.Count)];
                var duplicate = CreateItem(nextIdentity++);
                items = OrderedCollectionOperations.Duplicate(items, source.Id, random.Next(items.Count + 1), static current => current.Id, _ => duplicate);
            }
            else if (operation == 2 && items.Count > 1)
            {
                var item = items[random.Next(items.Count)];
                items = OrderedCollectionOperations.Remove(items, item.Id, static current => current.Id);
            }
            else
            {
                var item = CreateItem(nextIdentity++);
                items = OrderedCollectionOperations.Insert(items, item, random.Next(items.Count + 1), static current => current.Id);
            }

            Assert.NotEmpty(items);
            Assert.Equal(items.Count, items.Select(static item => item.Id).Distinct().Count());
            Assert.Throws<NotSupportedException>(() => ((IList<TestItem>)items).Add(CreateItem(nextIdentity)));
        }
    }

    private static ProjectMetadata CreateMetadata(string name = "Manual") => new(name, "es-MX", Timestamp, Timestamp.AddMinutes(1), 1);

    private static Step CreateStep(StepId stepId, AssetId assetId, int order)
    {
        var capture = new CaptureMetadata(
            assetId,
            new PhysicalPixelSize(1920, 1080),
            new CaptureDpi(96, 96),
            new MonitorIdentifier("DISPLAY1"),
            Timestamp);
        var interaction = new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            new NormalizedPoint(0.5, 0.5),
            Timestamp,
            order);
        var redaction = new RedactionAnnotation(
            AnnotationId.New(),
            0,
            new NormalizedRectangle(0.1, 0.1, 0.2, 0.2),
            RedactionStyle.Solid);

        return new Step(stepId, assetId, order, $"Paso {order}", "", capture, interaction, [redaction]);
    }

    private static TestItem CreateItem(int value) => new(Guid.Parse($"00000000-0000-0000-0000-{value:D12}"));

    private sealed record TestItem(Guid Id);
}
