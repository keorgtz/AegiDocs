using System.Reflection;
using AegiDocs.Domain.Geometry;
using AegiDocs.Domain.Interactions;
using Xunit;

namespace AegiDocs.Domain.Tests.Interactions;

public sealed class RecordedInteractionTests
{
    [Theory]
    [InlineData(MouseButton.Left)]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    [InlineData(MouseButton.XButton1)]
    [InlineData(MouseButton.XButton2)]
    public void ConstructorAcceptsEverySupportedMouseButton(MouseButton button)
    {
        var interaction = Create(button: button);

        Assert.Equal(InteractionType.MouseClick, interaction.Type);
        Assert.Equal(button, interaction.Button);
    }

    [Fact]
    public void ConstructorPreservesNormalizedPositionAndNormalizesTimestampToUtc()
    {
        var occurredAt = new DateTimeOffset(2026, 8, 5, 9, 15, 0, TimeSpan.FromHours(-5));
        var position = new NormalizedPoint(0.25d, 0.75d);

        var interaction = new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            position,
            occurredAt,
            sequence: 3);

        Assert.Equal(position, interaction.Position);
        Assert.Equal(occurredAt, interaction.OccurredAt);
        Assert.Equal(TimeSpan.Zero, interaction.OccurredAt.Offset);
        Assert.Equal(3, interaction.Sequence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void ConstructorRejectsUnsupportedMouseButtons(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(button: (MouseButton)value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorRejectsUnsupportedInteractionTypes(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedInteraction(
            (InteractionType)value,
            MouseButton.Left,
            new NormalizedPoint(0.5d, 0.5d),
            DateTimeOffset.UtcNow,
            sequence: 1));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void ConstructorRejectsNonPositiveSequence(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(sequence: sequence));
    }

    [Fact]
    public void ConstructorRejectsDefaultTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedInteraction(
            InteractionType.MouseClick,
            MouseButton.Left,
            new NormalizedPoint(0.5d, 0.5d),
            default,
            sequence: 1));
    }

    [Fact]
    public void ModelIsImmutableAndStructurallyExcludesSensitiveData()
    {
        var properties = typeof(RecordedInteraction).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.All(properties, property => Assert.False(property.SetMethod?.IsPublic ?? false));
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(string));
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(byte[]));

        var propertyNames = properties
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbiddenNames = new[]
        {
            "Key", "Keyboard", "Text", "Clipboard", "Control", "ControlName",
            "Process", "ProcessId", "ProcessName", "Window", "WindowTitle",
            "User", "UserName", "Email", "Path",
        };

        Assert.DoesNotContain(forbiddenNames, propertyNames.Contains);
    }

    private static RecordedInteraction Create(
        MouseButton button = MouseButton.Left,
        DateTimeOffset? occurredAt = null,
        long sequence = 1)
    {
        return new RecordedInteraction(
            InteractionType.MouseClick,
            button,
            new NormalizedPoint(0.5d, 0.5d),
            occurredAt ?? new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero),
            sequence);
    }
}
