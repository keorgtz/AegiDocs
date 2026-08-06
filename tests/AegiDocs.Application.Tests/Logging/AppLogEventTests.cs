using AegiDocs.Application.Logging;

namespace AegiDocs.Application.Tests.Logging;

public sealed class AppLogEventTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 8, 5, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorPreservesSafeMinimalEventData()
    {
        var correlationId = Guid.Parse("1d79b8ce-37fa-4f67-a0ed-d756f6f770de");

        var logEvent = new AppLogEvent(
            Timestamp,
            AppLogLevel.Warning,
            "Capture.Session",
            "Capture session stopped safely.",
            correlationId);

        Assert.Equal(Timestamp, logEvent.Timestamp);
        Assert.Equal(AppLogLevel.Warning, logEvent.Level);
        Assert.Equal("Capture.Session", logEvent.Category);
        Assert.Equal("Capture session stopped safely.", logEvent.Message);
        Assert.Equal(correlationId, logEvent.CorrelationId);
    }

    [Fact]
    public void ConstructorAllowsMissingCorrelationId()
    {
        var logEvent = CreateLogEvent();

        Assert.Null(logEvent.CorrelationId);
    }

    [Fact]
    public void NullLoggerDiscardsValidEvent()
    {
        NullAppLogger.Instance.Log(CreateLogEvent());
    }

    [Fact]
    public void NullLoggerRejectsMissingEvent()
    {
        Assert.Throws<ArgumentNullException>(() => NullAppLogger.Instance.Log(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Capture Session")]
    [InlineData("Capture/Session")]
    public void ConstructorRejectsInvalidCategory(string category)
    {
        Assert.Throws<ArgumentException>(() => new AppLogEvent(
            Timestamp,
            AppLogLevel.Information,
            category,
            "Capture session started."));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A message with\na line break.")]
    public void ConstructorRejectsInvalidMessage(string message)
    {
        Assert.Throws<ArgumentException>(() => new AppLogEvent(
            Timestamp,
            AppLogLevel.Information,
            "Capture.Session",
            message));
    }

    [Fact]
    public void ConstructorRejectsInvalidTimestampAndLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AppLogEvent(
            default,
            AppLogLevel.Information,
            "Capture.Session",
            "Capture session started."));
        Assert.Throws<ArgumentException>(() => new AppLogEvent(
            Timestamp.ToOffset(TimeSpan.FromHours(-6)),
            AppLogLevel.Information,
            "Capture.Session",
            "Capture session started."));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AppLogEvent(
            Timestamp,
            (AppLogLevel)999,
            "Capture.Session",
            "Capture session started."));
    }

    private static AppLogEvent CreateLogEvent() => new(
        Timestamp,
        AppLogLevel.Information,
        "Application.Test",
        "Test operation completed.");
}
