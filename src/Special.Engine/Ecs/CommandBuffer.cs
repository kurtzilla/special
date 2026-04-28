namespace Special.Engine.Ecs;

/// <summary>
/// Queues deferred entity destroys. <see cref="Flush"/> is the only path that strips all pools then recycles slots on the <see cref="Registry"/>.
/// </summary>
public sealed class CommandBuffer
{
    const int MinimumCapacity = 8;

    Entity[] _buffer;
    int _count;

    public CommandBuffer(int initialCapacity = 64)
    {
        var cap = initialCapacity <= 0 ? MinimumCapacity : initialCapacity;
        _buffer = new Entity[cap];
    }

    public int PendingCount => _count;

    /// <summary>Grow the queue so at least <paramref name="minCapacity"/> destroys can be queued without resizing.</summary>
    public void EnsureCapacity(int minCapacity)
    {
        if (minCapacity <= _buffer.Length)
            return;

        var newLen = _buffer.Length;
        while (newLen < minCapacity)
            newLen = newLen < 1024 ? Math.Max(newLen * 2, minCapacity) : newLen + 1024;

        Array.Resize(ref _buffer, newLen);
    }

    /// <summary>Queue a destroy; the entity stays alive until <see cref="Flush"/>.</summary>
    public void RequestDestroy(Entity entity)
    {
        if (_count >= _buffer.Length)
            GrowBuffer();

        _buffer[_count++] = entity;
    }

    /// <summary>Strip pools then recycle each queued entity slot. Clears the queue.</summary>
    public void Flush(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (_count == 0)
            return;

        for (var i = 0; i < _count; i++)
        {
            var entity = _buffer[i];
            if (!registry.IsAlive(entity))
                continue;

            registry.RemoveFromAllPools(entity);
            registry.Destroy(entity);
        }

        _count = 0;
    }

    void GrowBuffer()
    {
        var len = _buffer.Length;
        var newLen = len == 0 ? MinimumCapacity : len < 1024 ? len * 2 : len + 1024;
        Array.Resize(ref _buffer, newLen);
    }
}
