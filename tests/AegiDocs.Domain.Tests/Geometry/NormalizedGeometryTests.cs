using AegiDocs.Domain.Geometry;
using Xunit;

namespace AegiDocs.Domain.Tests.Geometry;

public sealed class NormalizedGeometryTests
{
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(1d, 1d)]
    [InlineData(0.25d, 0.75d)]
    public void PointAcceptsFiniteCoordinatesWithinInclusiveBounds(double x, double y)
    {
        var point = new NormalizedPoint(x, y);

        Assert.Equal(x, point.X);
        Assert.Equal(y, point.Y);
    }

    [Theory]
    [InlineData(-0.000001d)]
    [InlineData(1.000001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PointRejectsCoordinatesOutsideBoundsOrNotFinite(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedPoint(value, 0.5d));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedPoint(0.5d, value));
    }

    [Fact]
    public void PointConvertsToAndFromPixelsWithoutDpiInformation()
    {
        var point = NormalizedPoint.FromPixels(480d, 270d, 1920, 1080);
        var pixels = point.ToPixels(1920, 1080);

        Assert.Equal(0.25d, point.X);
        Assert.Equal(0.25d, point.Y);
        Assert.Equal(480d, pixels.X);
        Assert.Equal(270d, pixels.Y);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(-1, 1080)]
    [InlineData(1920, 0)]
    [InlineData(1920, -1)]
    public void PixelConversionsRejectNonPositiveDimensions(int pixelWidth, int pixelHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NormalizedPoint.FromPixels(0d, 0d, pixelWidth, pixelHeight));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedPoint(0.5d, 0.5d).ToPixels(pixelWidth, pixelHeight));
    }

    [Fact]
    public void RectangleAcceptsNonEmptyBoundsContainedWithinUnitSquare()
    {
        var rectangle = new NormalizedRectangle(0d, 0d, 1d, 1d);

        Assert.Equal(0d, rectangle.X);
        Assert.Equal(0d, rectangle.Y);
        Assert.Equal(1d, rectangle.Width);
        Assert.Equal(1d, rectangle.Height);
    }

    [Theory]
    [InlineData(-0.1d, 0d, 0.1d, 0.1d)]
    [InlineData(0d, -0.1d, 0.1d, 0.1d)]
    [InlineData(0d, 0d, 0d, 0.1d)]
    [InlineData(0d, 0d, 0.1d, 0d)]
    [InlineData(0d, 0d, -0.1d, 0.1d)]
    [InlineData(0d, 0d, 0.1d, -0.1d)]
    [InlineData(0.9d, 0d, 0.2d, 0.1d)]
    [InlineData(0d, 0.9d, 0.1d, 0.2d)]
    [InlineData(double.NaN, 0d, 0.1d, 0.1d)]
    [InlineData(0d, 0d, double.PositiveInfinity, 0.1d)]
    public void RectangleRejectsInvalidOrOutOfBoundsValues(double x, double y, double width, double height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NormalizedRectangle(x, y, width, height));
    }

    [Fact]
    public void RectangleRoundTripsThroughPixelsWithinFloatingPointTolerance()
    {
        var source = new NormalizedRectangle(0.125d, 0.2d, 0.625d, 0.5d);
        var pixels = source.ToPixels(1365, 768);
        var roundTripped = NormalizedRectangle.FromPixels(
            pixels.X,
            pixels.Y,
            pixels.Width,
            pixels.Height,
            1365,
            768);

        Assert.Equal(source.X, roundTripped.X, 12);
        Assert.Equal(source.Y, roundTripped.Y, 12);
        Assert.Equal(source.Width, roundTripped.Width, 12);
        Assert.Equal(source.Height, roundTripped.Height, 12);
    }
}
