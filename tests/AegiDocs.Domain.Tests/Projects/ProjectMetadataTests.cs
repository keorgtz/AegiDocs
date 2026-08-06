using AegiDocs.Domain.Projects;
using Xunit;

namespace AegiDocs.Domain.Tests.Projects;

public sealed class ProjectMetadataTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorNormalizesNameAndLanguageTag()
    {
        var metadata = Create(name: "  Manual\tde\nventas  ", languageTag: " es-mx ");

        Assert.Equal("Manual de ventas", metadata.Name);
        Assert.Equal("es-MX", metadata.LanguageTag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void ConstructorRejectsMissingName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(name: name!));
    }

    [Fact]
    public void ConstructorRejectsNameLongerThanMaximumAfterNormalization()
    {
        var name = new string('a', ProjectMetadata.MaximumNameLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Create(name: name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not_a_language")]
    public void ConstructorRejectsInvalidLanguageTag(string? languageTag)
    {
        Assert.ThrowsAny<ArgumentException>(() => Create(languageTag: languageTag!));
    }

    [Fact]
    public void ConstructorNormalizesTimestampsToUtcAndPreservesInstants()
    {
        var createdAt = new DateTimeOffset(2026, 8, 5, 7, 0, 0, TimeSpan.FromHours(-5));
        var modifiedAt = new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.FromHours(-5));

        var metadata = Create(createdAt: createdAt, modifiedAt: modifiedAt);

        Assert.Equal(TimeSpan.Zero, metadata.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, metadata.ModifiedAt.Offset);
        Assert.Equal(createdAt, metadata.CreatedAt);
        Assert.Equal(modifiedAt, metadata.ModifiedAt);
    }

    [Fact]
    public void ConstructorRejectsDefaultTimestampsAndModificationBeforeCreation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectMetadata(
            "Manual de ventas",
            "es-MX",
            default,
            CreatedAt.AddMinutes(1),
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectMetadata(
            "Manual de ventas",
            "es-MX",
            CreatedAt,
            default,
            1));
        Assert.Throws<ArgumentException>(() => Create(modifiedAt: CreatedAt.AddTicks(-1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsNonPositiveSchemaVersion(int schemaVersion)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(schemaVersion: schemaVersion));
    }

    [Fact]
    public void ConstructorPreservesPositiveSchemaVersionAndExposesNoPublicSetters()
    {
        var metadata = Create(schemaVersion: 1);

        Assert.Equal(1, metadata.SchemaVersion);
        Assert.All(
            typeof(ProjectMetadata).GetProperties(),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }

    private static ProjectMetadata Create(
        string name = "Manual de ventas",
        string languageTag = "es-MX",
        DateTimeOffset? createdAt = null,
        DateTimeOffset? modifiedAt = null,
        int schemaVersion = 1) => new(
        name,
        languageTag,
        createdAt ?? CreatedAt,
        modifiedAt ?? CreatedAt.AddMinutes(1),
        schemaVersion);
}
