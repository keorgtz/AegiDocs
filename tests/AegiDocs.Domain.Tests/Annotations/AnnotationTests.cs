using System.Reflection;
using AegiDocs.Domain.Annotations;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Identifiers;
using Xunit;

namespace AegiDocs.Domain.Tests.Annotations;

public sealed class AnnotationTests
{
    private static readonly AnnotationId AnnotationId = AnnotationId.Create(Guid.NewGuid());
    private static readonly NormalizedPoint Start = new(0.1d, 0.2d);
    private static readonly NormalizedPoint End = new(0.8d, 0.9d);
    private static readonly NormalizedRectangle Bounds = new(0.1d, 0.2d, 0.3d, 0.4d);
    private static readonly int[] ExpectedZIndexes = [0, 1, 2, 3, 4];

    [Fact]
    public void VariantsRetainStableIdentityZIndexAndSemanticGeometry()
    {
        Annotation[] annotations =
        [
            new NumberedMarkerAnnotation(AnnotationId, 0, Start, 1),
            new RectangleAnnotation(AnnotationId, 1, Bounds),
            new ArrowAnnotation(AnnotationId, 2, Start, End),
            new TextAnnotation(AnnotationId, 3, "Selecciona Guardar.", Bounds),
            new RedactionAnnotation(AnnotationId, 4, Bounds, RedactionStyle.Solid),
        ];

        Assert.All(annotations, annotation => Assert.Equal(AnnotationId, annotation.Id));
        Assert.Equal(ExpectedZIndexes, annotations.Select(annotation => annotation.ZIndex));
        Assert.Equal(Start, Assert.IsType<NumberedMarkerAnnotation>(annotations[0]).Position);
        Assert.Equal(Bounds, Assert.IsType<RectangleAnnotation>(annotations[1]).Bounds);
        Assert.Equal(End, Assert.IsType<ArrowAnnotation>(annotations[2]).End);
        Assert.Equal("Selecciona Guardar.", Assert.IsType<TextAnnotation>(annotations[3]).Content);
        Assert.Equal(RedactionStyle.Solid, Assert.IsType<RedactionAnnotation>(annotations[4]).Style);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void AllVariantsRejectNegativeZIndex(int zIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberedMarkerAnnotation(AnnotationId, zIndex, Start, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RectangleAnnotation(AnnotationId, zIndex, Bounds));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrowAnnotation(AnnotationId, zIndex, Start, End));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAnnotation(AnnotationId, zIndex, "Text", Bounds));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RedactionAnnotation(AnnotationId, zIndex, Bounds, RedactionStyle.Solid));
    }

    [Fact]
    public void AllVariantsRejectAnEmptyIdentity()
    {
        Assert.Throws<ArgumentException>(() => new NumberedMarkerAnnotation(default, 0, Start, 1));
        Assert.Throws<ArgumentException>(() => new RectangleAnnotation(default, 0, Bounds));
        Assert.Throws<ArgumentException>(() => new ArrowAnnotation(default, 0, Start, End));
        Assert.Throws<ArgumentException>(() => new TextAnnotation(default, 0, "Text", Bounds));
        Assert.Throws<ArgumentException>(() => new RedactionAnnotation(default, 0, Bounds, RedactionStyle.Solid));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NumberedMarkerRejectsNonPositiveNumbers(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberedMarkerAnnotation(AnnotationId, 0, Start, number));
    }

    [Fact]
    public void ArrowRejectsDegenerateGeometry()
    {
        Assert.Throws<ArgumentException>(() => new ArrowAnnotation(AnnotationId, 0, Start, Start));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void TextRejectsMissingOrWhitespaceContent(string? content)
    {
        Assert.Throws<ArgumentException>(() => new TextAnnotation(AnnotationId, 0, content!, Bounds));
    }

    [Fact]
    public void RedactionAcceptsTheSupportedSolidStyle()
    {
        var annotation = new RedactionAnnotation(AnnotationId, 0, Bounds, RedactionStyle.Solid);

        Assert.Equal(RedactionStyle.Solid, annotation.Style);
        Assert.Equal(Bounds, annotation.Bounds);
    }

    [Fact]
    public void RedactionRejectsUndefinedStyle()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RedactionAnnotation(AnnotationId, 0, Bounds, (RedactionStyle)1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RedactionAnnotation(AnnotationId, 0, Bounds, (RedactionStyle)99));
    }

    [Fact]
    public void AnnotationsExposeNoPublicSetters()
    {
        var annotationTypes = new[]
        {
            typeof(Annotation),
            typeof(NumberedMarkerAnnotation),
            typeof(RectangleAnnotation),
            typeof(ArrowAnnotation),
            typeof(TextAnnotation),
            typeof(RedactionAnnotation),
        };

        foreach (var annotationType in annotationTypes)
        {
            var declaredProperties = annotationType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.All(declaredProperties, property => Assert.Null(property.SetMethod));
        }
    }
}
