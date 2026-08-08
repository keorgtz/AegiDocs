using System.Reflection;
using System.Text.Json.Serialization;
using AegiDocs.Domain.Serialization;
using AegiDocs.Infrastructure.Storage.Persistence;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Persistence;

public sealed class ProjectPersistenceDtosTests
{
    [Fact]
    public void ManifestUsesStableSchemaAndCollectionMembers()
    {
        Assert.Equal(ProjectSchema.VersionPropertyName, JsonName<ProjectManifestDto>(nameof(ProjectManifestDto.SchemaVersion)));
        Assert.Equal("generation", JsonName<ProjectManifestDto>(nameof(ProjectManifestDto.Generation)));
        Assert.Equal("project", JsonName<ProjectManifestDto>(nameof(ProjectManifestDto.Project)));
        Assert.Equal("assets", JsonName<ProjectManifestDto>(nameof(ProjectManifestDto.Assets)));
        Assert.Empty(new ProjectManifestDto().Assets);
    }

    [Fact]
    public void AnnotationDtoRepresentsEveryV1KindWithStableDiscriminatorMember()
    {
        var kinds = new[]
        {
            ProjectSchema.NumberedMarkerAnnotationKind,
            ProjectSchema.RectangleAnnotationKind,
            ProjectSchema.ArrowAnnotationKind,
            ProjectSchema.TextAnnotationKind,
            ProjectSchema.RedactionAnnotationKind,
        };

        Assert.Equal(ProjectSchema.AnnotationDiscriminatorPropertyName, JsonName<AnnotationDto>(nameof(AnnotationDto.Kind)));
        Assert.Equal(kinds.Length, kinds.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("style", JsonName<AnnotationDto>(nameof(AnnotationDto.Style)));
    }

    [Fact]
    public void OptionalAuthorContentAndVariantFieldsAreExplicitlyNullable()
    {
        AssertNullable<TutorialDto>(nameof(TutorialDto.Description));
        AssertNullable<TutorialDto>(nameof(TutorialDto.Audience));
        AssertNullable<StepDto>(nameof(StepDto.Description));
        AssertNullable<AnnotationDto>(nameof(AnnotationDto.Content));
        AssertNullable<AnnotationDto>(nameof(AnnotationDto.Style));
        Assert.Null(new AnnotationDto().Content);
        Assert.Null(new AnnotationDto().Style);
    }

    [Fact]
    public void PersistenceDtosDoNotContainNativeHandlesOrAbsolutePathMembers()
    {
        var dtoTypes = typeof(ProjectManifestDto).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ProjectManifestDto).Namespace)
            .ToArray();

        Assert.DoesNotContain(dtoTypes.SelectMany(type => type.GetProperties()), property => property.PropertyType == typeof(IntPtr));
        Assert.DoesNotContain(dtoTypes.SelectMany(type => type.GetProperties()), property => property.Name.Contains("window", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dtoTypes.SelectMany(type => type.GetProperties()), property => property.Name.Contains("absolute", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("relativePath", JsonName<AssetReferenceDto>(nameof(AssetReferenceDto.RelativePath)));
    }

    private static string JsonName<T>(string propertyName) => typeof(T).GetProperty(propertyName)!
        .GetCustomAttribute<JsonPropertyNameAttribute>()!
        .Name;

    private static void AssertNullable<T>(string propertyName)
    {
        var nullability = new NullabilityInfoContext().Create(typeof(T).GetProperty(propertyName)!);
        Assert.Equal(NullabilityState.Nullable, nullability.WriteState);
    }
}
