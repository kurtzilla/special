namespace Special.Engine.Ecs.Query;

/// <summary>
/// Incrementally maintains entities that have both <typeparamref name="T1"/> and <typeparamref name="T2"/>.
/// Order is insertion order (swap-with-last on remove). Not thread-safe.
/// </summary>
internal sealed class QueryMatchSet<T1, T2> : IQueryMatchSink
    where T1 : struct
    where T2 : struct
{
    const int InitialMatches = 8;

    readonly ComponentPool<T1> _pool1;
    readonly ComponentPool<T2> _pool2;

    Entity[] _matches = new Entity[InitialMatches];
    int _count;

    /// <summary>Maps entity slot index → dense index in <see cref="_matches"/>, or -1 if not in the set.</summary>
    int[] _slotToDense = Array.Empty<int>();

    T1[] _staging1 = new T1[InitialMatches];
    T2[] _staging2 = new T2[InitialMatches];

    internal QueryMatchSet(ComponentPool<T1> pool1, ComponentPool<T2> pool2)
    {
        _pool1 = pool1;
        _pool2 = pool2;
    }

    /// <summary>Clears incremental state and repopulates from current pool contents (used when a query is registered after components already exist).</summary>
    internal void RebuildFromPools()
    {
        _count = 0;
        for (var s = 0; s < _slotToDense.Length; s++)
            _slotToDense[s] = -1;

        if (_pool1.Count <= _pool2.Count)
        {
            var entities = _pool1.Entities;
            for (var i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (_pool2.Contains(e))
                    TryAddMatch(e);
            }
        }
        else
        {
            var entities = _pool2.Entities;
            for (var i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (_pool1.Contains(e))
                    TryAddMatch(e);
            }
        }
    }

    internal int Count => _count;

    internal ReadOnlySpan<Entity> Matches => _matches.AsSpan(0, _count);

    /// <summary>Copies current component values for matched entities into staging and returns aligned spans (same length as <see cref="Matches"/>).</summary>
    internal AlignedQuerySpans<T1, T2> RefreshSpans()
    {
        EnsureStagingCapacity(_count);
        for (var i = 0; i < _count; i++)
        {
            var e = _matches[i];
            _ = _pool1.TryGet(e, out _staging1[i]);
            _ = _pool2.TryGet(e, out _staging2[i]);
        }

        return new AlignedQuerySpans<T1, T2>(_staging1.AsSpan(0, _count), _staging2.AsSpan(0, _count));
    }

    public void OnComponentStructural(Type componentType, Entity entity, bool added)
    {
        if (componentType != typeof(T1) && componentType != typeof(T2))
            return;

        if (added)
        {
            if (_pool1.Contains(entity) && _pool2.Contains(entity))
                TryAddMatch(entity);
        }
        else
            TryRemoveMatch(entity);
    }

    public void EnsureSlotMapCapacity(int minSlots)
    {
        if (minSlots <= 0)
            minSlots = 1;

        if (_slotToDense.Length >= minSlots)
            return;

        var oldLen = _slotToDense.Length;
        var newLen = _slotToDense.Length == 0 ? minSlots : _slotToDense.Length;
        while (newLen < minSlots)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        Array.Resize(ref _slotToDense, newLen);
        for (var i = oldLen; i < newLen; i++)
            _slotToDense[i] = -1;
    }

    void TryAddMatch(Entity entity)
    {
        var idx = (int)entity.Index;
        if (idx < 0 || idx >= _slotToDense.Length)
            return;

        if (_slotToDense[idx] >= 0 && _matches[_slotToDense[idx]] == entity)
            return;

        EnsureMatchCapacity(_count + 1);
        var dense = _count;
        _matches[dense] = entity;
        _slotToDense[idx] = dense;
        _count++;
    }

    void TryRemoveMatch(Entity entity)
    {
        var idx = (int)entity.Index;
        if (idx < 0 || idx >= _slotToDense.Length)
            return;

        var dense = _slotToDense[idx];
        if (dense < 0)
            return;

        if (_matches[dense] != entity)
        {
            _slotToDense[idx] = -1;
            return;
        }

        var last = _count - 1;
        if (dense != last)
        {
            var moved = _matches[last];
            _matches[dense] = moved;
            _slotToDense[(int)moved.Index] = dense;
        }

        _slotToDense[idx] = -1;
        _count--;
    }

    void EnsureMatchCapacity(int minCount)
    {
        if (_matches.Length >= minCount)
            return;

        var newLen = _matches.Length;
        while (newLen < minCount)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        Array.Resize(ref _matches, newLen);
    }

    void EnsureStagingCapacity(int minCount)
    {
        if (_staging1.Length >= minCount)
            return;

        var newLen = _staging1.Length;
        while (newLen < minCount)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        Array.Resize(ref _staging1, newLen);
        Array.Resize(ref _staging2, newLen);
    }
}
