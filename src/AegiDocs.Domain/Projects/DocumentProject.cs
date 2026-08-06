using System.Collections.ObjectModel;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Tutorials;

namespace AegiDocs.Domain.Projects;

/// <summary>
/// The immutable root aggregate for a locally authored documentation project.
/// </summary>
/// <remarks>
/// This aggregate contains only domain data. Asset paths, JSON representations,
/// UI state, synchronization, and I/O remain outside the domain so a project can
/// be safely reconstructed from DTOs and exercised without platform dependencies.
/// </remarks>
public sealed record DocumentProject
{
    /// <summary>
    /// Creates a validated document project.
    /// </summary>
    public DocumentProject(ProjectId id, ProjectMetadata metadata, IEnumerable<Tutorial>? tutorials = null)
    {
        if (id == default)
        {
            throw new ArgumentException("A project identifier is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(metadata);

        Id = id;
        Metadata = metadata;
        Tutorials = CreateOrderedTutorials(tutorials);
    }

    /// <summary>
    /// Gets the stable project identity.
    /// </summary>
    public ProjectId Id { get; }

    /// <summary>
    /// Gets immutable descriptive and schema metadata.
    /// </summary>
    public ProjectMetadata Metadata { get; }

    /// <summary>
    /// Gets tutorials in ascending, unique project order.
    /// </summary>
    public IReadOnlyList<Tutorial> Tutorials { get; }

    /// <summary>
    /// Returns a new project with replacement metadata.
    /// </summary>
    public DocumentProject WithMetadata(ProjectMetadata metadata) => new(Id, metadata, Tutorials);

    /// <summary>
    /// Returns a new project with a validated, deterministically ordered tutorial collection.
    /// </summary>
    public DocumentProject WithTutorials(IEnumerable<Tutorial>? tutorials) => new(Id, Metadata, tutorials);

    /// <summary>
    /// Returns a new project with one tutorial added.
    /// </summary>
    public DocumentProject AddTutorial(Tutorial tutorial)
    {
        ArgumentNullException.ThrowIfNull(tutorial);
        return new DocumentProject(Id, Metadata, Tutorials.Append(tutorial));
    }

    /// <summary>
    /// Returns a new project with an existing tutorial replaced by its identity.
    /// </summary>
    public DocumentProject ReplaceTutorial(Tutorial tutorial)
    {
        ArgumentNullException.ThrowIfNull(tutorial);

        if (!Tutorials.Any(current => current.Id == tutorial.Id))
        {
            throw new ArgumentException("The project does not contain the requested tutorial.", nameof(tutorial));
        }

        return new DocumentProject(Id, Metadata, Tutorials.Select(current => current.Id == tutorial.Id ? tutorial : current));
    }

    /// <summary>
    /// Returns a new project with an existing tutorial removed by identity.
    /// </summary>
    public DocumentProject RemoveTutorial(TutorialId tutorialId)
    {
        if (tutorialId == default)
        {
            throw new ArgumentException("A tutorial identifier is required.", nameof(tutorialId));
        }

        if (!Tutorials.Any(tutorial => tutorial.Id == tutorialId))
        {
            throw new ArgumentException("The project does not contain the requested tutorial.", nameof(tutorialId));
        }

        return new DocumentProject(Id, Metadata, Tutorials.Where(tutorial => tutorial.Id != tutorialId));
    }

    private static ReadOnlyCollection<Tutorial> CreateOrderedTutorials(IEnumerable<Tutorial>? tutorials)
    {
        var items = tutorials?.ToArray() ?? [];
        if (items.Any(static tutorial => tutorial is null))
        {
            throw new ArgumentException("A project tutorial collection cannot contain null values.", nameof(tutorials));
        }

        if (items.GroupBy(static tutorial => tutorial.Id).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("A project cannot contain tutorials with duplicate identifiers.", nameof(tutorials));
        }

        if (items.GroupBy(static tutorial => tutorial.Order).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("A project cannot contain tutorials with duplicate orders.", nameof(tutorials));
        }

        return new ReadOnlyCollection<Tutorial>(items.OrderBy(static tutorial => tutorial.Order).ToArray());
    }
}
