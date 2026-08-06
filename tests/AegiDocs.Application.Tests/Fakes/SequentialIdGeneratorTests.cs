namespace AegiDocs.Application.Tests.Fakes;

public sealed class SequentialIdGeneratorTests
{
    [Fact]
    public void CreateReturnsConfiguredIdentifiersInOrder()
    {
        var first = Guid.Parse("2f10bb5d-4c54-4b92-8c24-05b72f08670e");
        var second = Guid.Parse("4b8075a2-3c74-48b7-ad56-e004452e7de4");
        var generator = new SequentialIdGenerator([first, second]);

        Assert.Equal(first, generator.Create());
        Assert.Equal(second, generator.Create());
    }

    [Fact]
    public void CreateWhenSequenceIsExhaustedThrows()
    {
        var generator = new SequentialIdGenerator([]);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = generator.Create());

        Assert.Equal("The configured identifier sequence is exhausted.", exception.Message);
    }
}
