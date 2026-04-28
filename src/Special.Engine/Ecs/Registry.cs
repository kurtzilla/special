using System.Diagnostics.CodeAnalysis;
using Special.Engine.Ecs.Query;

namespace Special.Engine.Ecs;

/// <summary>
/// Entity slot data: generations, LIFO free-index stack (<see cref="_freeIndices"/> + <see cref="_freeCount"/>), and component pools.
/// Pool stripping for deferred teardown is invoked only via <see cref="RemoveFromAllPools"/> (called from <see cref="CommandBuffer.Flush"/>).
/// </summary>
public sealed class Registry
{
    const int InitialCapacity = 8;

    uint[] _generations = new uint[InitialCapacity];
    /// <summary>LIFO stack of recycled slot indices (array + count, not a growable list of ints).</summary>
    int[] _freeIndices = new int[InitialCapacity];
    int _freeCount;
    int _slotCount;

    readonly Dictionary<Type, IComponentPool> _pools = new();
    readonly Dictionary<Type, Delegate> _componentCloners = new();
    readonly List<IQueryMatchSink> _querySinks = new();
    readonly Dictionary<(Type T1, Type T2), object> _queryPairs = new();

    /// <summary>Number of entity slots ever allocated (exclusive upper bound on <see cref="Entity.Index"/>).</summary>
    public int SlotCount => _slotCount;

    /// <summary>Creates a new entity with a unique handle.</summary>
    public Entity CreateEntity()
    {
        if (_freeCount > 0)
        {
            var index = _freeIndices[--_freeCount];
            var gen = _generations[index];
            return Entity.FromParts((uint)index, gen);
        }

        var newIndex = _slotCount;
        EnsureSlotCapacity(newIndex + 1);
        _generations[newIndex] = 1;
        _slotCount = newIndex + 1;
        EnsureAllQuerySlotMaps(_slotCount);
        return Entity.FromParts((uint)newIndex, 1);
    }

    /// <summary>
    /// Returns a cached query for entities that have both components. Creates pools and match tracking on first use.
    /// Match order is insertion order when an entity gains the second component of the pair.
    /// </summary>
    public Query<T1, T2> Query<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        var key = (typeof(T1), typeof(T2));
        if (!_queryPairs.TryGetValue(key, out var boxed))
        {
            var p1 = GetPool<T1>();
            var p2 = GetPool<T2>();
            var set = new QueryMatchSet<T1, T2>(p1, p2);
            _queryPairs[key] = set;
            _querySinks.Add(set);
            set.EnsureSlotMapCapacity(Math.Max(_slotCount, 1));
            set.RebuildFromPools();
            boxed = set;
        }

        var matchSet = (QueryMatchSet<T1, T2>)boxed;
        return new Query<T1, T2>(GetPool<T1>(), GetPool<T2>(), matchSet);
    }

    internal void NotifyComponentStructuralChange<T>(Entity entity, bool added)
        where T : struct
    {
        var t = typeof(T);
        for (var i = 0; i < _querySinks.Count; i++)
            _querySinks[i].OnComponentStructural(t, entity, added);
    }

    void EnsureAllQuerySlotMaps(int minSlots)
    {
        for (var i = 0; i < _querySinks.Count; i++)
            _querySinks[i].EnsureSlotMapCapacity(minSlots);
    }

    /// <summary>
    /// Recycles the entity slot (generation bump + free stack). Does not strip component pools.
    /// Invoked only from <see cref="CommandBuffer.Flush"/> after <see cref="RemoveFromAllPools"/>.
    /// </summary>
    internal void Destroy(Entity entity)
    {
        if (!IsAlive(entity))
            return;

        var index = (int)entity.Index;
        var gen = _generations[index];
        gen = gen >= Entity.MaxGeneration ? 1u : gen + 1u;
        _generations[index] = gen;

        EnsureFreeCapacity(_freeCount + 1);
        _freeIndices[_freeCount++] = index;
    }

    /// <summary>True if this handle matches the current generation for its index.</summary>
    public bool IsAlive(Entity entity)
    {
        if (!entity.IsValid)
            return false;

        var index = (int)entity.Index;
        if (index < 0 || index >= _slotCount)
            return false;

        return _generations[index] == entity.Generation && entity.Generation != 0;
    }

    /// <summary>
    /// Registers a copy function for <b>reference-bearing</b> struct components only (where
    /// <c>RuntimeHelpers.IsReferenceOrContainsReferences&lt;T&gt;()</c> is true — e.g. <see cref="string"/>, collections, reference-type arrays).
    /// Do <b>not</b> use this for pure POD components; see <see cref="ComponentCloneRegistration"/> for policy and engine defaults.
    /// Stored in an internal dictionary keyed by <see cref="Type"/>. Used by <see cref="IComponentPool.TryCloneFromTo"/> (see <see cref="Clone"/>).
    /// If no cloner is registered for such a type, a shallow struct copy still runs and DEBUG logs a warning.
    /// If many unrelated types all need cloners, consider refactoring data into buffers, pools, or handles instead of growing this table.
    /// </summary>
    public void RegisterComponentCloner<T>(Func<T, T> clone)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(clone);
        _componentCloners[typeof(T)] = clone;
    }

    internal bool TryGetComponentCloner<T>(out Func<T, T>? clone)
        where T : struct
    {
        if (_componentCloners.TryGetValue(typeof(T), out var d) && d is Func<T, T> f)
        {
            clone = f;
            return true;
        }

        clone = null;
        return false;
    }

    /// <summary>Creates an entity and applies the template’s component steps in order.</summary>
    public Entity Instantiate(EntityTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var entity = CreateEntity();
        template.Apply(this, entity);
        return entity;
    }

    /// <summary>
    /// Creates a new entity and copies every component row present on <paramref name="source"/> from each registered pool.
    /// For each pool: a registered <see cref="RegisterComponentCloner{T}"/> is applied when the struct may contain references; otherwise a default value copy is used.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is not alive.</exception>
    public Entity Clone(Entity source)
    {
        if (!IsAlive(source))
            throw new ArgumentException("Source entity is not alive.", nameof(source));

        var destination = CreateEntity();
        foreach (var pool in _pools.Values)
            _ = pool.TryCloneFromTo(source, destination);

        return destination;
    }

    /// <summary>Gets or creates the dense pool for <typeparamref name="T"/>; cache at system init.</summary>
    public ComponentPool<T> GetPool<T>() where T : struct
    {
        var key = typeof(T);
        if (_pools.TryGetValue(key, out var existing))
            return (ComponentPool<T>)existing;

        var pool = new ComponentPool<T>(this);
        _pools[key] = pool;
        return pool;
    }

    /// <summary>Used by <see cref="EntityCommandBuffer.Playback"/> to resolve pools without a compile-time <c>T</c>.</summary>
    internal bool TryGetPool(Type componentType, [NotNullWhen(true)] out IComponentPool? pool)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        return _pools.TryGetValue(componentType, out pool);
    }

    /// <summary>Creates the pool for <paramref name="componentType"/> on first use, mirroring <see cref="GetPool{T}"/>.</summary>
    internal IComponentPool GetOrCreatePool(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!componentType.IsValueType)
            throw new ArgumentException("Component type must be a struct.", nameof(componentType));

        if (_pools.TryGetValue(componentType, out var existing))
            return existing;

        var poolType = typeof(ComponentPool<>).MakeGenericType(componentType);
        var pool = (IComponentPool)Activator.CreateInstance(poolType, this)!;
        _pools[componentType] = pool;
        return pool;
    }

    /// <summary>Strips this entity from every registered pool. Called only from <see cref="CommandBuffer.Flush"/>.</summary>
    internal void RemoveFromAllPools(Entity entity)
    {
        foreach (var pool in _pools.Values)
            pool.RemoveForEntityIfPresent(entity);
    }

    void EnsureSlotCapacity(int minLength)
    {
        if (_generations.Length >= minLength)
            return;

        var newLen = _generations.Length;
        while (newLen < minLength)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        Array.Resize(ref _generations, newLen);
        Array.Resize(ref _freeIndices, Math.Max(newLen, _freeIndices.Length));
    }

    void EnsureFreeCapacity(int minFreeArrayUsed)
    {
        if (_freeIndices.Length >= minFreeArrayUsed)
            return;

        var newLen = _freeIndices.Length * 2;
        while (newLen < minFreeArrayUsed)
            newLen *= 2;

        Array.Resize(ref _freeIndices, newLen);
    }
}
