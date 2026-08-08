using System.Reflection;
using AegiDocs.Domain.Captures;
using AegiDocs.Domain.Identifiers;
using Xunit;

namespace AegiDocs.Domain.Tests.Captures;

public sealed class CaptureMetadataTests
{
    [Fact]
    public void ConstructorPreservesValidatedTechnicalMetadataAndNormalizesTimestampToUtc()
    {
        var capturedAt = new DateTimeOffset(2026, 8, 5, 7, 30, 0, TimeSpan.FromHours(-5));
        var assetId = AssetId.New();
        var metadata = new CaptureMetadata(
            assetId,
            new PhysicalPixelSize(3840, 2160),
            new CaptureDpi(144d, 144d),
            new MonitorIdentifier("  display-technical-id  "),
            capturedAt);

        Assert.Equal(assetId, metadata.AssetId);
        Assert.Equal(3840, metadata.PixelSize.Width);
        Assert.Equal(2160, metadata.PixelSize.Height);
        Assert.Equal(144d, metadata.Dpi.Horizontal);
        Assert.Equal(144d, metadata.Dpi.Vertical);
        Assert.Equal("display-technical-id", metadata.MonitorIdentifier.Value);
        Assert.Equal(TimeSpan.Zero, metadata.CapturedAt.Offset);
        Assert.Equal(capturedAt, metadata.CapturedAt);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(-1, 1080)]
    [InlineData(1920, 0)]
    [InlineData(1920, -1)]
    public void PhysicalPixelSizeRejectsNonPositiveDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicalPixelSize(width, height));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void CaptureDpiRejectsNonPositiveOrNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureDpi(value, 96d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureDpi(96d, value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void MonitorIdentifierRejectsMissingValues(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new MonitorIdentifier(value!));
    }

    [Fact]
    public void ConstructorRejectsDefaultTimestampEmptyAssetIdentifierAndMissingMonitorIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new CaptureMetadata(
            default,
            new PhysicalPixelSize(1, 1),
            new CaptureDpi(96d, 96d),
            new MonitorIdentifier("display-technical-id"),
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentNullException>(() => new CaptureMetadata(
            AssetId.New(),
            new PhysicalPixelSize(1, 1),
            new CaptureDpi(96d, 96d),
            null!,
            DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureMetadata(
            AssetId.New(),
            new PhysicalPixelSize(1, 1),
            new CaptureDpi(96d, 96d),
            new MonitorIdentifier("display-technical-id"),
            default));
    }

    [Fact]
    public void MetadataIsImmutableAndExposesNoSensitiveTextualFields()
    {
        Assert.All(
            typeof(CaptureMetadata).GetProperties(),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));

        var propertyNames = typeof(CaptureMetadata)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbiddenNames = new[] { "Window", "WindowTitle", "ProcessName", "ProcessId", "Path", "FilePath", "UserName" };

        Assert.DoesNotContain(forbiddenNames, name => propertyNames.Contains(name));
        Assert.DoesNotContain(typeof(CaptureMetadata).GetProperties(), property => property.PropertyType == typeof(string));
        Assert.DoesNotContain(typeof(CaptureMetadata).GetProperties(), property => property.PropertyType == typeof(IntPtr));
        Assert.Null(typeof(CaptureMetadata).Assembly.GetType("AegiDocs.Domain.Captures.WindowReference"));
    }
}
