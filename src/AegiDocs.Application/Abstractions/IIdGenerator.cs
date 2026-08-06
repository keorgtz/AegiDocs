namespace AegiDocs.Application.Abstractions;

/// <summary>
/// Creates unique identifiers for application workflows.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Creates a new identifier.
    /// </summary>
    Guid Create();
}
