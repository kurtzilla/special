using System.Collections.Concurrent;

namespace Special.Engine.Collision;

/// <summary>
/// Thread-safe producer buffer for collision events emitted by parallel detection jobs.
/// </summary>
public sealed class CollisionEventBuffer
{
    readonly ConcurrentBag<CollisionEvent> _events = new();

    public void Add(in CollisionEvent collisionEvent) => _events.Add(collisionEvent);

    public void DrainTo(List<CollisionEvent> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        while (_events.TryTake(out var collisionEvent))
            destination.Add(collisionEvent);
    }

    public void Clear()
    {
        while (_events.TryTake(out _))
        {
        }
    }
}
