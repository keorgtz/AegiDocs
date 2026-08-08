using AegiDocs.Domain.Serialization;
using Xunit;

namespace AegiDocs.Domain.Tests.Serialization;

public sealed class ProjectSchemaTests
{
    [Fact]
    public void InitialVersionIsTheCurrentPositiveVersion()
    {
        Assert.Equal(1, ProjectSchema.InitialVersion);
        Assert.Equal(ProjectSchema.InitialVersion, ProjectSchema.CurrentVersion);
        Assert.True(ProjectSchema.CurrentVersion > 0);
    }

    [Fact]
    public void ContractPropertyAndAnnotationDiscriminatorsAreStableAndDistinct()
    {
        var annotationKinds = new[]
        {
            ProjectSchema.NumberedMarkerAnnotationKind,
            ProjectSchema.RectangleAnnotationKind,
            ProjectSchema.ArrowAnnotationKind,
            ProjectSchema.TextAnnotationKind,
            ProjectSchema.RedactionAnnotationKind,
        };

        Assert.Equal("schemaVersion", ProjectSchema.VersionPropertyName);
        Assert.Equal("kind", ProjectSchema.AnnotationDiscriminatorPropertyName);
        Assert.Equal(annotationKinds.Length, annotationKinds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(annotationKinds, kind => Assert.False(string.IsNullOrWhiteSpace(kind)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidVersionsAreClassifiedAndRejected(int version)
    {
        Assert.Equal(SchemaReadCompatibility.Invalid, ProjectSchema.GetReadCompatibility(version));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProjectSchema.EnsureReadable(version));
    }

    [Fact]
    public void CurrentVersionIsAcceptedWithoutMigration()
    {
        Assert.Equal(SchemaReadCompatibility.Current, ProjectSchema.GetReadCompatibility(ProjectSchema.CurrentVersion));

        ProjectSchema.EnsureReadable(ProjectSchema.CurrentVersion);
    }

    [Fact]
    public void FutureVersionsAreRejectedBeforeTheyCanBeMapped()
    {
        var futureVersion = ProjectSchema.CurrentVersion + 1;

        Assert.Equal(SchemaReadCompatibility.UnsupportedFuture, ProjectSchema.GetReadCompatibility(futureVersion));
        Assert.Throws<NotSupportedException>(() => ProjectSchema.EnsureReadable(futureVersion));
    }
}
