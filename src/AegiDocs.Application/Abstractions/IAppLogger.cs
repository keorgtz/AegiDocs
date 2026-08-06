using AegiDocs.Application.Logging;

namespace AegiDocs.Application.Abstractions;

/// <summary>
/// Receives minimal, safe application diagnostics.
/// </summary>
/// <remarks>
/// The operation is synchronous because this port only transfers an already-created immutable
/// event to its implementation. Buffered or asynchronous I/O belongs to the infrastructure
/// implementation and must not block an application workflow.
/// </remarks>
public interface IAppLogger
{
    /// <summary>
    /// Records a safe local diagnostic event.
    /// </summary>
    void Log(AppLogEvent logEvent);
}
