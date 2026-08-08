using AegiDocs.Domain.Identifiers;
using AegiDocs.Infrastructure.Storage.Assets;
using AegiDocs.Infrastructure.Storage.Persistence;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Assets;

public sealed class AssetStoreTests
{
    [Fact]
    public async Task StoreAsyncDerivesCanonicalRelativePngPathWithoutCallerPath()
    {
        var fileSystem = new FakeAssetFileSystem();
        var store = new AssetStore(fileSystem);
        var assetId = AssetId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var result = await store.StoreAsync(CreateRequest(assetId, 42, ".png"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("assets/11111111-1111-1111-1111-111111111111/v0000000000000042.png", result.Asset!.RelativePath);
        Assert.DoesNotContain(":", result.Asset.RelativePath, StringComparison.Ordinal);
        Assert.DoesNotContain("..", result.Asset.RelativePath, StringComparison.Ordinal);
        Assert.Equal(result.Asset.RelativePath, fileSystem.WrittenPath);
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".PNG")]
    [InlineData("../../capture.png")]
    public async Task StoreAsyncRejectsUnsupportedExtensionsBeforeFilesystemAccess(string extension)
    {
        var fileSystem = new FakeAssetFileSystem();
        var result = await new AssetStore(fileSystem).StoreAsync(CreateRequest(AssetId.New(), 1, extension), CancellationToken.None);

        Assert.Equal(AssetStoreStatus.UnsupportedExtension, result.Status);
        Assert.Equal(0, fileSystem.InventoryCalls);
    }

    [Fact]
    public async Task StoreAsyncEnforcesAssetAndProjectBudgets()
    {
        var tooLarge = await new AssetStore(new FakeAssetFileSystem()).StoreAsync(
            CreateRequest(AssetId.New(), 1, ".png", AssetStoreLimits.MaximumAssetBytes + 1),
            CancellationToken.None);
        var fullProject = await new AssetStore(new FakeAssetFileSystem(new(AssetStoreLimits.MaximumAssetCount, 0))).StoreAsync(
            CreateRequest(AssetId.New(), 1, ".png"),
            CancellationToken.None);
        var fullBytes = await new AssetStore(new FakeAssetFileSystem(new(0, AssetStoreLimits.MaximumTotalAssetBytes))).StoreAsync(
            CreateRequest(AssetId.New(), 1, ".png"),
            CancellationToken.None);

        Assert.Equal(AssetStoreStatus.AssetTooLarge, tooLarge.Status);
        Assert.Equal(AssetStoreStatus.AssetLimitExceeded, fullProject.Status);
        Assert.Equal(AssetStoreStatus.TotalBytesLimitExceeded, fullBytes.Status);
    }

    [Fact]
    public async Task StoreAsyncDeduplicatesUsingOptionalSha256()
    {
        var existing = new AssetReferenceDto { AssetId = Guid.NewGuid().ToString("D"), Revision = 1, RelativePath = "assets/existing/v0000000000000001.png" };
        var fileSystem = new FakeAssetFileSystem(existingByHash: existing);

        var result = await new AssetStore(fileSystem).StoreAsync(CreateRequest(AssetId.New(), 1, ".png", calculateHash: true), CancellationToken.None);

        Assert.Equal(AssetStoreStatus.Deduplicated, result.Status);
        Assert.Same(existing, result.Asset);
        Assert.Null(fileSystem.WrittenPath);
        Assert.NotNull(fileSystem.LastHash);
    }

    [Fact]
    public async Task StoreAsyncHonorsCancellationWithoutWriting()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var fileSystem = new FakeAssetFileSystem();

        var result = await new AssetStore(fileSystem).StoreAsync(CreateRequest(AssetId.New(), 1, ".png"), cancellationSource.Token);

        Assert.Equal(AssetStoreStatus.Canceled, result.Status);
        Assert.Null(fileSystem.WrittenPath);
    }

    private static AssetWriteRequest CreateRequest(AssetId assetId, long revision, string extension, long? contentLength = null, bool calculateHash = false)
    {
        var content = new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1]);
        return new AssetWriteRequest(assetId, revision, extension, content, contentLength ?? content.Length, calculateHash);
    }

    private sealed class FakeAssetFileSystem : IAssetStoreFileSystem
    {
        private readonly AssetStoreInventory inventory;
        private readonly AssetReferenceDto? existingByHash;

        public FakeAssetFileSystem(AssetStoreInventory? inventory = null, AssetReferenceDto? existingByHash = null)
        {
            this.inventory = inventory ?? new AssetStoreInventory(0, 0);
            this.existingByHash = existingByHash;
        }

        public int InventoryCalls { get; private set; }
        public string? WrittenPath { get; private set; }
        public string? LastHash { get; private set; }

        public ValueTask<AssetStoreInventory> GetInventoryAsync(CancellationToken cancellationToken)
        {
            InventoryCalls++;
            return ValueTask.FromResult(inventory);
        }

        public ValueTask<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public ValueTask<AssetReferenceDto?> FindByContentHashAsync(string sha256Hex, CancellationToken cancellationToken)
        {
            LastHash = sha256Hex;
            return ValueTask.FromResult(existingByHash);
        }

        public ValueTask WriteAsync(string relativePath, Stream content, CancellationToken cancellationToken)
        {
            WrittenPath = relativePath;
            return ValueTask.CompletedTask;
        }
    }
}
