using AegiDocs.Application.Abstractions;

namespace AegiDocs.Application;

/// <summary>
/// Creates identifiers with the platform GUID generator.
/// </summary>
public sealed class GuidIdGenerator : IIdGenerator
{
    /// <inheritdoc />
    public Guid Create() => Guid.NewGuid();
}
