using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Operations;
using Xunit;

namespace AegiDocs.Domain.Tests.Operations;

public sealed class OrderedCollectionOperationsTests
{
    [Fact]
    public void InsertSupportsAllBoundaryPositionsAndDoesNotMutateInput()
    {
        var first = new StepItem(StepId.New(), "first");
        var second = new StepItem(StepId.New(), "second");
        var inserted = new StepItem(StepId.New(), "inserted");
        var source = new List<StepItem> { first, second };

        var atStart = OrderedCollectionOperations.Insert(source, inserted, 0, static item => item.Id);
        var atEnd = OrderedCollectionOperations.Insert(source, inserted, source.Count, static item => item.Id);
        source.Clear();

        Assert.Equal([inserted, first, second], atStart);
        Assert.Equal([first, second, inserted], atEnd);
        Assert.Throws<NotSupportedException>(() => ((IList<StepItem>)atStart).Add(new StepItem(StepId.New(), "other")));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void InsertRejectsInvalidPosition(int position)
    {
        var source = new[] { new StepItem(StepId.New(), "first") };

        Assert.Throws<ArgumentOutOfRangeException>(() => OrderedCollectionOperations.Insert(
            source,
            new StepItem(StepId.New(), "second"),
            position,
            static item => item.Id));
    }

    [Fact]
    public void InsertRejectsAnIdentityAlreadyPresent()
    {
        var first = new StepItem(StepId.New(), "first");

        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Insert(
            [first],
            first with { Label = "copy" },
            1,
            static item => item.Id));
    }

    [Fact]
    public void MoveIsDeterministicAcrossRepeatedOperationsAndPreservesInput()
    {
        var first = new TutorialItem(TutorialId.New(), "first");
        var second = new TutorialItem(TutorialId.New(), "second");
        var third = new TutorialItem(TutorialId.New(), "third");
        var source = new[] { first, second, third };

        var once = OrderedCollectionOperations.Move(source, third.Id, 0, static item => item.Id);
        var twice = OrderedCollectionOperations.Move(once, first.Id, 2, static item => item.Id);

        Assert.Equal([third, first, second], once);
        Assert.Equal([third, second, first], twice);
        Assert.Equal([first, second, third], source);
    }

    [Fact]
    public void MoveRejectsMissingDefaultAndOutOfRangeInputs()
    {
        var first = new TutorialItem(TutorialId.New(), "first");

        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Move([first], default, 0, static item => item.Id));
        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Move([first], TutorialId.New(), 0, static item => item.Id));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderedCollectionOperations.Move([first], first.Id, -1, static item => item.Id));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderedCollectionOperations.Move([first], first.Id, 1, static item => item.Id));
    }

    [Fact]
    public void DuplicateRequiresAnExplicitFactoryWithNewIdentity()
    {
        var source = new StepItem(StepId.New(), "source");

        var duplicated = OrderedCollectionOperations.Duplicate(
            [source],
            source.Id,
            1,
            static item => item.Id,
            item => item with { Id = StepId.New(), Label = $"{item.Label}-copy" });

        Assert.Equal(2, duplicated.Count);
        Assert.Equal(source, duplicated[0]);
        Assert.NotEqual(source.Id, duplicated[1].Id);
        Assert.Equal("source-copy", duplicated[1].Label);
    }

    [Fact]
    public void DuplicateRejectsFactoryThatReusesAnyExistingIdentity()
    {
        var first = new StepItem(StepId.New(), "first");
        var second = new StepItem(StepId.New(), "second");

        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Duplicate(
            [first, second],
            first.Id,
            1,
            static item => item.Id,
            static item => item));
        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Duplicate(
            [first, second],
            first.Id,
            1,
            static item => item.Id,
            _ => second));
    }

    [Fact]
    public void RemoveReturnsNewCollectionAndRejectsMissingIdentity()
    {
        var first = new TutorialItem(TutorialId.New(), "first");
        var second = new TutorialItem(TutorialId.New(), "second");
        var source = new[] { first, second };

        var removed = OrderedCollectionOperations.Remove(source, first.Id, static item => item.Id);

        Assert.Equal([second], removed);
        Assert.Equal([first, second], source);
        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Remove(source, TutorialId.New(), static item => item.Id));
    }

    [Fact]
    public void OperationsRejectDuplicateAndNullSourceItems()
    {
        var duplicate = new TutorialItem(TutorialId.New(), "duplicate");
        TutorialItem?[] nullSource = [new TutorialItem(TutorialId.New(), "first"), null];

        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Remove(
            [duplicate, duplicate with { Label = "second" }],
            duplicate.Id,
            static item => item.Id));
        Assert.Throws<ArgumentException>(() => OrderedCollectionOperations.Remove(
            nullSource!,
            duplicate.Id,
            static item => item!.Id));
    }

    private sealed record StepItem(StepId Id, string Label);

    private sealed record TutorialItem(TutorialId Id, string Label);
}
