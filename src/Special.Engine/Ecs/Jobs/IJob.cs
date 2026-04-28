namespace Special.Engine.Ecs.Jobs;

/// <summary>
/// Parallel-schedulable unit of work. <see cref="ReadOnlyComponents"/> and <see cref="WriteOnlyComponents"/> drive
/// <see cref="DependencyResolver"/> batching (topological layers). Lists must be stable for the tick (typically <c>static readonly</c> backing).
/// </summary>
public interface IJob
{
    /// <summary>Component types this job reads only (no structural pool changes).</summary>
    IReadOnlyList<Type> ReadOnlyComponents { get; }

    /// <summary>Component types this job may write (structural or slot mutation).</summary>
    IReadOnlyList<Type> WriteOnlyComponents { get; }

    /// <summary>
    /// Invoked from a <see cref="System.Threading.ThreadPool"/> worker (via TPL); there is no fixed mapping from job/system instance to OS thread.
    /// Implementations must be thread-agnostic and respect declared access; do not touch other pools unsafely.
    /// </summary>
    void Execute(in JobContext context);
}
