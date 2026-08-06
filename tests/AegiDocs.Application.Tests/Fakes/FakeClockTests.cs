namespace AegiDocs.Application.Tests.Fakes;

public sealed class FakeClockTests
{
    [Fact]
    public void SetNormalizesValueToUtc()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var time = new DateTimeOffset(2026, 8, 5, 8, 30, 0, TimeSpan.FromHours(-6));

        clock.Set(time);

        Assert.Equal(time.ToUniversalTime(), clock.UtcNow);
        Assert.Equal(TimeSpan.Zero, clock.UtcNow.Offset);
    }

    [Fact]
    public void AdvanceMovesTimeByRequestedDuration()
    {
        var initial = new DateTimeOffset(2026, 8, 5, 14, 30, 0, TimeSpan.Zero);
        var clock = new FakeClock(initial);

        clock.Advance(TimeSpan.FromMinutes(90));

        Assert.Equal(initial.AddMinutes(90), clock.UtcNow);
    }
}
