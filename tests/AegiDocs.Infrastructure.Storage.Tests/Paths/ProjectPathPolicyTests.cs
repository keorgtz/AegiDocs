using AegiDocs.Infrastructure.Storage.Paths;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Paths;

public sealed class ProjectPathPolicyTests
{
    [Theory]
    [InlineData("assets/0123/v0000000000000001.png")]
    [InlineData("recovery/g0001/manifest.json")]
    public void TryValidateRelativePathAcceptsCanonicalRelativeSegments(string path)
    {
        Assert.True(ProjectPathPolicy.TryValidateRelativePath(path, out var canonical));
        Assert.Equal(path, canonical);
    }

    [Theory]
    [InlineData("../manifest.json")]
    [InlineData("assets\\file.png")]
    [InlineData("C:/outside.png")]
    [InlineData("//server/share/file.png")]
    [InlineData("\\\\?\\C:\\device.png")]
    [InlineData("assets//file.png")]
    [InlineData("assets/./file.png")]
    public void TryValidateRelativePathRejectsTraversalAndAmbiguousPaths(string path)
    {
        Assert.False(ProjectPathPolicy.TryValidateRelativePath(path, out _));
    }

    [Fact]
    public void IsSafeUnderRootRejectsReparsePointAndHandleEscapingRoot()
    {
        var root = "C:/projects/example.aegidocs";
        var policy = new FakeReparsePolicy { FinalPathUnderRoot = false };

        Assert.False(ProjectPathPolicy.IsSafeUnderRoot(root, "C:/outside/manifest.json", policy));

        policy.FinalPathUnderRoot = true;
        policy.ReparsePaths.Add(root);
        Assert.False(ProjectPathPolicy.IsSafeUnderRoot(root, root + "/manifest.json", policy));
    }

    private sealed class FakeReparsePolicy : IReparsePointPolicy
    {
        public HashSet<string> ReparsePaths { get; } = new(StringComparer.Ordinal);
        public bool FinalPathUnderRoot { get; set; }
        public bool IsReparsePoint(string canonicalPath) => ReparsePaths.Contains(canonicalPath);
        public bool IsFinalHandleUnderRoot(string canonicalRoot, string finalHandlePath) => FinalPathUnderRoot;
    }
}
