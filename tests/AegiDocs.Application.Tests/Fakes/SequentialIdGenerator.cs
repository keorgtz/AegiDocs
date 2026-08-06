using AegiDocs.Application.Abstractions;

namespace AegiDocs.Application.Tests.Fakes;

internal sealed class SequentialIdGenerator(IEnumerable<Guid> ids) : IIdGenerator
{
    private readonly Queue<Guid> _ids = new(ids);

    public Guid Create()
    {
        if (_ids.Count == 0)
        {
            throw new InvalidOperationException("The configured identifier sequence is exhausted.");
        }

        return _ids.Dequeue();
    }
}
