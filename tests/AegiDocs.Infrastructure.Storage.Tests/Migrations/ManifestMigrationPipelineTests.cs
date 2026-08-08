using AegiDocs.Infrastructure.Storage.Migrations;
using System.Text.Json.Nodes;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Migrations;

public sealed class ManifestMigrationPipelineTests
{
    [Fact]
    public void CurrentSchemaIsReturnedUnchangedAndIsIdempotent()
    {
        var pipeline = new ManifestMigrationPipeline();
        var manifest = CreateManifest(1, 1);

        var first = pipeline.Migrate(manifest);
        var second = pipeline.Migrate(first.Manifest!);

        Assert.True(first.Succeeded);
        Assert.Equal(ManifestMigrationStatus.Current, first.Status);
        Assert.Same(manifest, first.Manifest);
        Assert.Equal(ManifestMigrationStatus.Current, second.Status);
        Assert.Same(manifest, second.Manifest);
    }

    [Fact]
    public void FutureAndInvalidSchemasAreRejectedBeforeMigration()
    {
        var pipeline = new ManifestMigrationPipeline();

        Assert.Equal(ManifestMigrationStatus.InvalidSchema, pipeline.Preflight(0));
        Assert.Equal(ManifestMigrationStatus.UnsupportedFutureSchema, pipeline.Preflight(2));
        Assert.Equal(ManifestMigrationStatus.InvalidSchema, pipeline.Migrate(CreateManifest(0, 1)).Status);
        Assert.Equal(ManifestMigrationStatus.UnsupportedFutureSchema, pipeline.Migrate(CreateManifest(2, 1)).Status);
    }

    [Fact]
    public void PublicPipelineRejectsMigratorsBeyondTheCurrentSchema()
    {
        Assert.Throws<ArgumentException>(() => new ManifestMigrationPipeline([new RenamePropertyMigrator()]));
    }

    [Fact]
    public void InvalidOrMutatedGenerationProducesContentSafeFailure()
    {
        var pipeline = new ManifestMigrationPipeline();

        Assert.Equal(ManifestMigrationStatus.InvalidGeneration, pipeline.Migrate(CreateManifest(1, 0)).Status);
        Assert.Equal(ManifestMigrationStatus.InvalidGeneration, pipeline.Migrate(CreateManifest(1, -1)).Status);

        var mutated = new GenerationMutatingMigrator().Migrate(CreateManifest(1, 7));
        Assert.False(ManifestMigrationPipeline.IsValidMigratorOutput(mutated, 2, 7));
    }

    [Fact]
    public void JsonMigratorCanRenameAndRemovePropertiesWhilePreservingUnrelatedContent()
    {
        var migrator = new RenamePropertyMigrator();
        var manifest = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["generation"] = 7,
            ["legacyTitle"] = "Synthetic title",
            ["obsolete"] = true,
            ["preserved"] = new JsonObject { ["nested"] = "Synthetic value" },
        };

        var migrated = migrator.Migrate(manifest);

        Assert.Equal("Synthetic title", migrated["title"]!.GetValue<string>());
        Assert.False(migrated.ContainsKey("legacyTitle"));
        Assert.False(migrated.ContainsKey("obsolete"));
        Assert.Equal("Synthetic value", migrated["preserved"]!["nested"]!.GetValue<string>());
        Assert.Equal(7, migrated["generation"]!.GetValue<int>());
    }

    private static JsonObject CreateManifest(int schemaVersion, long generation) => new()
    {
        ["schemaVersion"] = schemaVersion,
        ["generation"] = generation,
    };

    private sealed class RenamePropertyMigrator : IManifestMigrator
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public JsonObject Migrate(JsonObject manifest)
        {
            var migrated = manifest.DeepClone().AsObject();
            migrated["title"] = migrated["legacyTitle"]?.DeepClone();
            migrated.Remove("legacyTitle");
            migrated.Remove("obsolete");
            migrated["schemaVersion"] = ToVersion;
            return migrated;
        }
    }

    private sealed class GenerationMutatingMigrator : IManifestMigrator
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public JsonObject Migrate(JsonObject manifest)
        {
            var migrated = manifest.DeepClone().AsObject();
            migrated["schemaVersion"] = ToVersion;
            migrated["generation"] = 8;
            return migrated;
        }
    }
}
