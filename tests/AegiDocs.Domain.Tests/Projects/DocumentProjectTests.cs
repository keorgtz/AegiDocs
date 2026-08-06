using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Projects;
using AegiDocs.Domain.Tutorials;
using Xunit;

namespace AegiDocs.Domain.Tests.Projects;

public sealed class DocumentProjectTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorOrdersTutorialsAndExposesImmutableCollection()
    {
        var second = CreateTutorial(order: 2);
        var first = CreateTutorial(order: 1);
        var project = CreateProject(tutorials: [second, first]);

        Assert.Equal([first, second], project.Tutorials);
        Assert.Throws<NotSupportedException>(() => ((IList<Tutorial>)project.Tutorials).Add(CreateTutorial(order: 3)));
    }

    [Fact]
    public void ConstructorMaterializesTutorialsWithoutRetainingCallerCollection()
    {
        var tutorials = new List<Tutorial> { CreateTutorial() };
        var project = CreateProject(tutorials: tutorials);
        tutorials.Clear();

        Assert.Single(project.Tutorials);
    }

    [Fact]
    public void ConstructorRejectsDefaultIdentityNullMetadataAndInvalidTutorialCollections()
    {
        var tutorial = CreateTutorial();

        Assert.Throws<ArgumentException>(() => new DocumentProject(default, CreateMetadata()));
        Assert.Throws<ArgumentNullException>(() => new DocumentProject(ProjectId.New(), null!));
        Assert.Throws<ArgumentException>(() => CreateProject(tutorials: [tutorial, tutorial]));
        Assert.Throws<ArgumentException>(() => CreateProject(tutorials: [tutorial, CreateTutorial(order: tutorial.Order, id: TutorialId.New())]));
        Assert.Throws<ArgumentException>(() => CreateProject(tutorials: new Tutorial[] { null! }));
    }

    [Fact]
    public void WithMetadataReturnsNewProjectAndPreservesTutorials()
    {
        var tutorial = CreateTutorial();
        var project = CreateProject(tutorials: [tutorial]);
        var updatedMetadata = CreateMetadata(name: "Nuevo manual", modifiedAt: CreatedAt.AddHours(1));

        var updated = project.WithMetadata(updatedMetadata);

        Assert.NotSame(project, updated);
        Assert.Equal(updatedMetadata, updated.Metadata);
        Assert.Equal([tutorial], updated.Tutorials);
    }

    [Fact]
    public void TutorialOperationsReturnNewValidatedProjects()
    {
        var first = CreateTutorial(order: 1, title: "Primero");
        var second = CreateTutorial(order: 2, title: "Segundo");
        var replacement = CreateTutorial(id: first.Id, order: 1, title: "Actualizado");
        var project = CreateProject(tutorials: [first]);

        var added = project.AddTutorial(second);
        var replaced = added.ReplaceTutorial(replacement);
        var removed = replaced.RemoveTutorial(second.Id);

        Assert.Equal([first], project.Tutorials);
        Assert.Equal([first, second], added.Tutorials);
        Assert.Equal([replacement, second], replaced.Tutorials);
        Assert.Equal([replacement], removed.Tutorials);
    }

    [Fact]
    public void TutorialOperationsRejectMissingOrConflictingTutorials()
    {
        var tutorial = CreateTutorial();
        var project = CreateProject(tutorials: [tutorial]);

        Assert.Throws<ArgumentException>(() => project.AddTutorial(CreateTutorial(id: tutorial.Id, order: 2)));
        Assert.Throws<ArgumentException>(() => project.AddTutorial(CreateTutorial(order: tutorial.Order)));
        Assert.Throws<ArgumentException>(() => project.ReplaceTutorial(CreateTutorial()));
        Assert.Throws<ArgumentException>(() => project.RemoveTutorial(TutorialId.New()));
        Assert.Throws<ArgumentException>(() => project.RemoveTutorial(default));
    }

    private static DocumentProject CreateProject(
        ProjectId? id = null,
        ProjectMetadata? metadata = null,
        IEnumerable<Tutorial>? tutorials = null) => new(id ?? ProjectId.New(), metadata ?? CreateMetadata(), tutorials);

    private static ProjectMetadata CreateMetadata(
        string name = "Manual de ventas",
        DateTimeOffset? modifiedAt = null) => new(name, "es-MX", CreatedAt, modifiedAt ?? CreatedAt.AddMinutes(1), 1);

    private static Tutorial CreateTutorial(TutorialId? id = null, int order = 1, string title = "Tutorial") => new(
        id ?? TutorialId.New(),
        order,
        title,
        "",
        "");
}
