using AegiDocs.Infrastructure.Storage.Limits;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Limits;

public sealed class StorageLimitsTests
{
    [Fact]
    public void ValidateAllowsEveryMaximumBoundary()
    {
        Assert.Equal(StorageLimitStatus.Allowed, Validate());
    }

    [Theory]
    [InlineData(true, StorageLimitStatus.ArchiveUnsupported)]
    [InlineData(false, StorageLimitStatus.Allowed)]
    public void ValidateHandlesArchivePolicy(bool archive, StorageLimitStatus expected)
    {
        Assert.Equal(expected, Validate(archive: archive));
    }

    [Fact]
    public void ValidateRejectsEachExceededBudgetAndNegativeInput()
    {
        Assert.Equal(StorageLimitStatus.ManifestTooLarge, Validate(manifestBytes: StorageLimits.MaximumManifestBytes + 1));
        Assert.Equal(StorageLimitStatus.InvalidJsonBudget, Validate(jsonDepth: StorageLimits.MaximumJsonDepth + 1));
        Assert.Equal(StorageLimitStatus.CollectionLimit, Validate(steps: StorageLimits.MaximumSteps + 1));
        Assert.Equal(StorageLimitStatus.AssetLimit, Validate(totalAssetBytes: StorageLimits.MaximumTotalAssetBytes + 1));
        Assert.Equal(StorageLimitStatus.ImageLimit, Validate(width: 10_000, height: 10_000));
        Assert.Equal(StorageLimitStatus.PathLimit, Validate(pathLength: StorageLimits.MaximumPathLength + 1));
        Assert.Equal(StorageLimitStatus.RecoveryLimit, Validate(recovery: StorageLimits.MaximumRecoveryCount + 1));
        Assert.Equal(StorageLimitStatus.TemporaryLimit, Validate(temporary: StorageLimits.MaximumTemporaryCount + 1));
        Assert.Equal(StorageLimitStatus.InvalidInput, Validate(assets: -1));
    }

    private static StorageLimitStatus Validate(
        long manifestBytes = StorageLimits.MaximumManifestBytes,
        int jsonDepth = StorageLimits.MaximumJsonDepth,
        int stringLength = StorageLimits.MaximumStringLength,
        int tutorials = StorageLimits.MaximumTutorials,
        int steps = StorageLimits.MaximumSteps,
        int annotations = StorageLimits.MaximumAnnotations,
        int assets = StorageLimits.MaximumAssets,
        long assetBytes = StorageLimits.MaximumAssetBytes,
        long totalAssetBytes = StorageLimits.MaximumTotalAssetBytes,
        int width = 5760,
        int height = 5760,
        int pathDepth = StorageLimits.MaximumPathDepth,
        int pathLength = StorageLimits.MaximumPathLength,
        int recovery = StorageLimits.MaximumRecoveryCount,
        int temporary = StorageLimits.MaximumTemporaryCount,
        bool archive = false) => StorageLimits.Validate(manifestBytes, jsonDepth, stringLength, tutorials, steps, annotations, assets, assetBytes, totalAssetBytes, width, height, pathDepth, pathLength, recovery, temporary, archive);
}
