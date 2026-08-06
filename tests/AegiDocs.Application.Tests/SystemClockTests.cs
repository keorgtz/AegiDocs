namespace AegiDocs.Application.Tests;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNowReturnsUtcValue()
    {
        var clock = new SystemClock();

        var result = clock.UtcNow;

        Assert.Equal(TimeSpan.Zero, result.Offset);
    }
}
