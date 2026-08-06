using AegiDocs.Application.Abstractions;

namespace AegiDocs.Application.Tests.Fakes;

internal sealed class FakeClock(DateTimeOffset initialUtcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = EnsureUtc(initialUtcNow);

    public void Set(DateTimeOffset utcNow)
    {
        UtcNow = EnsureUtc(utcNow);
    }

    public void Advance(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value) => value.ToUniversalTime();
}
