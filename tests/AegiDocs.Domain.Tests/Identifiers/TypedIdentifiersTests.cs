using System.Reflection;
using AegiDocs.Domain.Identifiers;
using Xunit;

namespace AegiDocs.Domain.Tests.Identifiers;

public sealed class TypedIdentifiersTests
{
    [Fact]
    public void NewCreatesNonEmptyIds()
    {
        Assert.NotEqual(Guid.Empty, ProjectId.New().Value);
        Assert.NotEqual(Guid.Empty, TutorialId.New().Value);
        Assert.NotEqual(Guid.Empty, StepId.New().Value);
        Assert.NotEqual(Guid.Empty, AssetId.New().Value);
        Assert.NotEqual(Guid.Empty, AnnotationId.New().Value);
    }

    [Fact]
    public void CreatePreservesProvidedValueAndUsesTypedEquality()
    {
        var value = Guid.NewGuid();

        Assert.Equal(ProjectId.Create(value), ProjectId.Create(value));
        Assert.Equal(TutorialId.Create(value), TutorialId.Create(value));
        Assert.Equal(StepId.Create(value), StepId.Create(value));
        Assert.Equal(AssetId.Create(value), AssetId.Create(value));
        Assert.Equal(AnnotationId.Create(value), AnnotationId.Create(value));
        Assert.NotEqual(ProjectId.Create(value), ProjectId.Create(Guid.NewGuid()));
        Assert.NotEqual(TutorialId.Create(value), TutorialId.Create(Guid.NewGuid()));
        Assert.NotEqual(StepId.Create(value), StepId.Create(Guid.NewGuid()));
        Assert.NotEqual(AssetId.Create(value), AssetId.Create(Guid.NewGuid()));
        Assert.NotEqual(AnnotationId.Create(value), AnnotationId.Create(Guid.NewGuid()));
    }

    [Fact]
    public void CreateRejectsEmptyGuidForEachIdentifierType()
    {
        Assert.Throws<ArgumentException>(() => ProjectId.Create(Guid.Empty));
        Assert.Throws<ArgumentException>(() => TutorialId.Create(Guid.Empty));
        Assert.Throws<ArgumentException>(() => StepId.Create(Guid.Empty));
        Assert.Throws<ArgumentException>(() => AssetId.Create(Guid.Empty));
        Assert.Throws<ArgumentException>(() => AnnotationId.Create(Guid.Empty));
    }

    [Fact]
    public void ParseAndTryParseAcceptNonEmptyGuidValues()
    {
        var value = Guid.NewGuid();
        var text = value.ToString();

        Assert.Equal(ProjectId.Create(value), ProjectId.Parse(text));
        Assert.Equal(TutorialId.Create(value), TutorialId.Parse(text));
        Assert.Equal(StepId.Create(value), StepId.Parse(text));
        Assert.Equal(AssetId.Create(value), AssetId.Parse(text));
        Assert.Equal(AnnotationId.Create(value), AnnotationId.Parse(text));
        Assert.True(ProjectId.TryParse(text, out var projectId));
        Assert.True(TutorialId.TryParse(text, out var tutorialId));
        Assert.True(StepId.TryParse(text, out var stepId));
        Assert.True(AssetId.TryParse(text, out var assetId));
        Assert.True(AnnotationId.TryParse(text, out var annotationId));
        Assert.Equal(value, projectId.Value);
        Assert.Equal(value, tutorialId.Value);
        Assert.Equal(value, stepId.Value);
        Assert.Equal(value, assetId.Value);
        Assert.Equal(value, annotationId.Value);
    }

    [Fact]
    public void ParseAndTryParseRejectInvalidAndEmptyValues()
    {
        Assert.Throws<FormatException>(() => ProjectId.Parse("invalid"));
        Assert.Throws<FormatException>(() => TutorialId.Parse("invalid"));
        Assert.Throws<FormatException>(() => StepId.Parse("invalid"));
        Assert.Throws<FormatException>(() => AssetId.Parse("invalid"));
        Assert.False(ProjectId.TryParse(Guid.Empty.ToString(), out _));
        Assert.False(TutorialId.TryParse("invalid", out _));
        Assert.False(StepId.TryParse(null, out _));
        Assert.False(AssetId.TryParse(string.Empty, out _));
        Assert.False(AnnotationId.TryParse(Guid.Empty.ToString(), out _));
    }

    [Fact]
    public void IdentifierTypesAreDistinctAndDoNotDeclareImplicitConversions()
    {
        Assert.NotEqual(typeof(ProjectId), typeof(TutorialId));
        Assert.NotEqual(typeof(TutorialId), typeof(StepId));
        Assert.NotEqual(typeof(StepId), typeof(AssetId));
        Assert.NotEqual(typeof(AssetId), typeof(AnnotationId));

        var identifierTypes = new[] { typeof(ProjectId), typeof(TutorialId), typeof(StepId), typeof(AssetId), typeof(AnnotationId) };
        foreach (var identifierType in identifierTypes)
        {
            var implicitConversions = identifierType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "op_Implicit");

            Assert.Empty(implicitConversions);
        }
    }
}
