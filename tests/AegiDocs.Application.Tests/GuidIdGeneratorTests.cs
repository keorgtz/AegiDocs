namespace AegiDocs.Application.Tests;

public sealed class GuidIdGeneratorTests
{
    [Fact]
    public void CreateReturnsNonEmptyUniqueIdentifiers()
    {
        var generator = new GuidIdGenerator();

        var first = generator.Create();
        var second = generator.Create();

        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(Guid.Empty, second);
        Assert.NotEqual(first, second);
    }
}
