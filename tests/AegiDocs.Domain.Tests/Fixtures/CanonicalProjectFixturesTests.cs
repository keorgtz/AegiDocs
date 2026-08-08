using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Serialization;
using Xunit;

namespace AegiDocs.Domain.Tests.Fixtures;

public sealed class CanonicalProjectFixturesTests
{
    [Fact]
    public void EmptyProjectIsCurrentSchemaAndContainsNoTutorials()
    {
        var project = CanonicalProjectFixtures.CreateEmptyProject();

        Assert.Equal(ProjectSchema.CurrentVersion, project.Metadata.SchemaVersion);
        Assert.Empty(project.Tutorials);
    }

    [Fact]
    public void SimpleTutorialContainsOneConsistentCapturedStep()
    {
        var tutorial = CanonicalProjectFixtures.CreateSimpleTutorial();
        var step = Assert.Single(tutorial.Steps);

        Assert.Equal(1, tutorial.Order);
        Assert.Equal(1, step.Order);
        Assert.Equal(step.AssetId, step.Capture.AssetId);
        Assert.Empty(step.Annotations);
    }

    [Fact]
    public void CompleteAnnotationsProjectContainsEverySupportedSemanticAnnotation()
    {
        var project = CanonicalProjectFixtures.CreateCompleteAnnotationsProject();
        var annotations = Assert.Single(Assert.Single(project.Tutorials).Steps).Annotations;

        Assert.Collection(
            annotations,
            static annotation => Assert.IsType<NumberedMarkerAnnotation>(annotation),
            static annotation => Assert.IsType<RectangleAnnotation>(annotation),
            static annotation => Assert.IsType<ArrowAnnotation>(annotation),
            static annotation => Assert.IsType<TextAnnotation>(annotation),
            static annotation => Assert.IsType<RedactionAnnotation>(annotation));
        var redaction = Assert.IsType<RedactionAnnotation>(annotations[^1]);
        Assert.Equal(RedactionStyle.Solid, redaction.Style);
    }

    [Fact]
    public void LargeProjectHasDeterministicOrdersAndCanBeConstructedRepeatedly()
    {
        var first = CanonicalProjectFixtures.CreateLargeProject();
        var second = CanonicalProjectFixtures.CreateLargeProject();

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Metadata, second.Metadata);
        Assert.Equal(
            first.Tutorials.Select(static tutorial => tutorial.Id),
            second.Tutorials.Select(static tutorial => tutorial.Id));
        Assert.Equal(16, first.Tutorials.Count);
        Assert.All(first.Tutorials, static tutorial => Assert.Equal(24, tutorial.Steps.Count));
        Assert.Equal(Enumerable.Range(1, 16), first.Tutorials.Select(static tutorial => tutorial.Order));
        Assert.All(first.Tutorials, static tutorial => Assert.Equal(
            Enumerable.Range(1, 24),
            tutorial.Steps.Select(static step => step.Order)));
    }
}
