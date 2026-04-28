namespace Special.Engine.Ecs.Query;

/// <summary>
/// Cached view of entities that have both <typeparamref name="T1"/> and <typeparamref name="T2"/>.
/// Use <see cref="RefreshSpans"/> for aligned SoA copies; use <see cref="Iterate"/> for ref access without staging.
/// </summary>
public readonly struct Query<T1, T2>
    where T1 : struct
    where T2 : struct
{
    readonly ComponentPool<T1> _pool1;
    readonly ComponentPool<T2> _pool2;
    readonly QueryMatchSet<T1, T2> _matches;

    internal Query(ComponentPool<T1> pool1, ComponentPool<T2> pool2, QueryMatchSet<T1, T2> matches)
    {
        _pool1 = pool1;
        _pool2 = pool2;
        _matches = matches;
    }

    /// <summary>Number of entities with both components.</summary>
    public int Count => _matches.Count;

    /// <summary>Matched entities in query-dense order (not the same order as either pool’s dense arrays).</summary>
    public ReadOnlySpan<Entity> Entities => _matches.Matches;

    /// <summary>
    /// Copies current values from pools into internal staging; returned spans are aligned by index with <see cref="Entities"/>.
    /// Values reflect pool state at call time (call again after writes or structural changes).
    /// </summary>
    public AlignedQuerySpans<T1, T2> RefreshSpans() => _matches.RefreshSpans();

    /// <summary>Stack-only iterator over matches; <see cref="QueryIterator{T1,T2}.Current1"/> / <see cref="QueryIterator{T1,T2}.Current2"/> are invalid after the next structural pool change.</summary>
    public QueryIterator<T1, T2> Iterate() => new(_pool1, _pool2, _matches.Matches);

    /// <summary>Supports <c>foreach</c> over matches (same as <see cref="Iterate"/>).</summary>
    public QueryIterator<T1, T2> GetEnumerator() => Iterate();
}

/// <summary>Per-row view for <c>foreach</c>; refs are only valid until the next structural change to either pool.</summary>
public readonly ref struct QueryRow<T1, T2>
    where T1 : struct
    where T2 : struct
{
    readonly ComponentPool<T1> _pool1;
    readonly ComponentPool<T2> _pool2;
    readonly Entity _entity;

    internal QueryRow(ComponentPool<T1> pool1, ComponentPool<T2> pool2, Entity entity)
    {
        _pool1 = pool1;
        _pool2 = pool2;
        _entity = entity;
    }

    /// <summary>Entity for this row.</summary>
    public Entity Entity => _entity;

    /// <summary>Writable ref into the first pool.</summary>
    public ref T1 Component1 => ref _pool1.GetRef(_entity);

    /// <summary>Writable ref into the second pool.</summary>
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
}

/// <summary>Ref struct enumerator for a <see cref="Query{T1,T2}"/>; supports <c>foreach</c> via <see cref="Current"/>.</summary>
public ref struct QueryIterator<T1, T2>
    where T1 : struct
    where T2 : struct
{
    readonly ComponentPool<T1> _pool1;
    readonly ComponentPool<T2> _pool2;
    readonly ReadOnlySpan<Entity> _entities;
    int _index;

    internal QueryIterator(ComponentPool<T1> pool1, ComponentPool<T2> pool2, ReadOnlySpan<Entity> entities)
    {
        _pool1 = pool1;
        _pool2 = pool2;
        _entities = entities;
        _index = -1;
    }

    /// <summary>Advances to the next match. Returns false when finished.</summary>
    public bool MoveNext()
    {
        _index++;
        return _index < _entities.Length;
    }

    /// <summary>Row for the current iteration; only valid after <see cref="MoveNext"/> returns true.</summary>
    public QueryRow<T1, T2> Current => new(_pool1, _pool2, _entities[_index]);

    /// <summary>Writable ref into pool storage; only valid until the next structural change to <typeparamref name="T1"/>.</summary>
    public ref T1 Current1 => ref _pool1.GetRef(_entities[_index]);

    /// <summary>Writable ref into pool storage; only valid until the next structural change to <typeparamref name="T2"/>.</summary>
    public ref T2 Current2 => ref _pool2.GetRef(_entities[_index]);

    /// <summary>Current entity handle for this iteration step.</summary>
    public readonly Entity CurrentEntity => _entities[_index];
}
