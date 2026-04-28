#if DEBUG
using System.Diagnostics;
using System.Threading;
using DiagnosticsDebug = System.Diagnostics.Debug;
#endif
using System.Runtime.CompilerServices;

namespace Special.Engine.Ecs;

/// <summary>
/// Dense SoA storage for one component type: contiguous <typeparamref name="T"/> in <c>T[]</c>, O(1) add/remove via swap-with-last.
/// Indexed by <see cref="Entity.Index"/> through a reverse map. Not thread-safe.
/// Hot paths (<see cref="TryAdd"/>, <see cref="TryGet"/>, <see cref="Remove"/>, etc.) do not allocate; implicit dense-array growth invokes the optional capacity callback if set.
/// </summary>
public sealed class ComponentPool<T> : IComponentPool where T : struct
{
    const int InitialValues = 8;
    const int NoDenseRow = -1;

    readonly Registry _registry;
    readonly Action<int, int>? _onCapacityGrew;

    T[] _values;
    Entity[] _rows;
    int[] _denseRowByEntitySlot = Array.Empty<int>();
    int _count;

#if DEBUG
    int _debugStructuralMutationDepth;

    void EnterStructuralMutation()
    {
        var d = Interlocked.Increment(ref _debugStructuralMutationDepth);
        DiagnosticsDebug.Assert(d == 1, "Overlapping structural mutation on ComponentPool (concurrent writers or re-entrancy).");
    }

    void ExitStructuralMutation() => Interlocked.Decrement(ref _debugStructuralMutationDepth);
#endif
    /// <param name="registry">Registry used for <see cref="Registry.IsAlive"/> and <see cref="Registry.SlotCount"/>.</param>
    /// <param name="onCapacityGrew">Optional callback when dense <c>T[]</c> / <c>Entity[]</c> grow implicitly (e.g. from <see cref="TryAdd"/>); arguments are <c>(oldLength, newLength)</c>. Not invoked from <see cref="Resize"/>.</param>
    public ComponentPool(Registry registry, Action<int, int>? onCapacityGrew = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _onCapacityGrew = onCapacityGrew;
        _values = new T[InitialValues];
        _rows = new Entity[InitialValues];
    }

    /// <summary>How many entities currently have this component.</summary>
    public int Count => _count;

    /// <summary>Dense component values (length <see cref="Count"/>).</summary>
    public ReadOnlySpan<T> Values => _values.AsSpan(0, _count);

    /// <summary>Entity handle for each dense row (same length as <see cref="Values"/>).</summary>
    public ReadOnlySpan<Entity> Entities => _rows.AsSpan(0, _count);

    /// <summary>
    /// Ensures dense value buffers hold at least <paramref name="minimumCapacity"/> elements and the slot map covers the registry’s current slot count.
    /// Does not trigger <see cref="OnCapacityGrew"/>; use during initialization to avoid implicit growth during simulation.
    /// </summary>
    public void Resize(int minimumCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumCapacity);

#if DEBUG
        EnterStructuralMutation();
        try
        {
#endif
            EnsureSlotMap(_registry.SlotCount);

            if (_values.Length >= minimumCapacity)
                return;

            GrowDenseArrays(minimumCapacity, notifyImplicitGrow: false);
#if DEBUG
        }
        finally
        {
            ExitStructuralMutation();
        }
#endif
    }

    public bool Contains(Entity entity)
    {
        if (!_registry.IsAlive(entity))
            return false;

        var idx = (int)entity.Index;
        if (idx < 0 || idx >= _denseRowByEntitySlot.Length)
            return false;

        var dense = _denseRowByEntitySlot[idx];
        return dense != NoDenseRow && _rows[dense] == entity;
    }

    public bool TryGet(Entity entity, out T value)
    {
        if (!TryGetDenseRow(entity, out var dense))
        {
            value = default;
            return false;
        }

        value = _values[dense];
        return true;
    }

    /// <summary>Writable reference to the component; only valid until the next structural change to this pool.</summary>
    public ref T GetRef(Entity entity)
    {
        if (!TryGetDenseRow(entity, out var dense))
            throw new InvalidOperationException("Entity does not have this component or handle is stale.");

        return ref _values[dense];
    }

    /// <summary>Dense-row random access for cache-friendly scans; <paramref name="denseIndex"/> in <c>[0, Count)</c>.</summary>
    public ref T GetRefAtDense(int denseIndex)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(denseIndex, 0);
        if (denseIndex >= _count)
            throw new ArgumentOutOfRangeException(nameof(denseIndex));

        return ref _values[denseIndex];
    }

    /// <summary>Adds a component row for a live entity that does not already have one.</summary>
    public bool TryAdd(Entity entity, in T value)
    {
        if (!_registry.IsAlive(entity))
            return false;

#if DEBUG
        EnterStructuralMutation();
        try
        {
#endif
            EnsureSlotMap(_registry.SlotCount);
            var idx = (int)entity.Index;
            if (_denseRowByEntitySlot[idx] != NoDenseRow)
                return false;

            EnsureDenseCapacityForCount(_count + 1);
            var dense = _count;
            _values[dense] = value;
            _rows[dense] = entity;
            _denseRowByEntitySlot[idx] = dense;
            _count++;
            _registry.NotifyComponentStructuralChange<T>(entity, added: true);
            return true;
#if DEBUG
        }
        finally
        {
            ExitStructuralMutation();
        }
#endif
    }

    /// <inheritdoc />
    public bool TryAddFromObject(Entity entity, object boxedComponent)
    {
        if (boxedComponent is not T t)
            return false;

        return TryAdd(entity, in t);
    }

    /// <summary>Removes this component immediately (swap-with-last). Does not destroy the entity.</summary>
    public void Remove(Entity entity) => RemoveAtEntitySlot(entity);

    /// <inheritdoc />
    public void RemoveForEntityIfPresent(Entity entity) => RemoveAtEntitySlot(entity);

    /// <inheritdoc />
    public bool TryCloneFromTo(Entity source, Entity destination)
    {
        if (!TryGet(source, out var value))
            return false;

#if DEBUG
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() &&
            (!_registry.TryGetComponentCloner<T>(out var registered) || registered is null))
        {
            DiagnosticsDebug.WriteLine(
                $"[Special.Engine.Ecs] Clone: struct component '{typeof(T).FullName}' contains reference fields but no " +
                $"{nameof(Registry.RegisterComponentCloner)} was registered for this type; shallow copy will alias those references.");
        }
#endif

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && _registry.TryGetComponentCloner<T>(out var cloner) && cloner is not null)
            value = cloner(value);

        if (Contains(destination))
            Remove(destination);

        if (!TryAdd(destination, value))
            throw new InvalidOperationException($"Failed to clone component {typeof(T)} to destination entity.");

        return true;
    }

    void RemoveAtEntitySlot(Entity entity)
    {
        if (!_registry.IsAlive(entity))
            return;

        var idx = (int)entity.Index;
        if (idx < 0 || idx >= _denseRowByEntitySlot.Length)
            return;

        var dense = _denseRowByEntitySlot[idx];
        if (dense == NoDenseRow || _rows[dense] != entity)
            return;

#if DEBUG
        EnterStructuralMutation();
        try
        {
#endif
            var last = _count - 1;
            if (dense != last)
            {
                _values[dense] = _values[last];
                var moved = _rows[last];
                _rows[dense] = moved;
                _denseRowByEntitySlot[(int)moved.Index] = dense;
            }

            _denseRowByEntitySlot[idx] = NoDenseRow;
            _count--;
            _registry.NotifyComponentStructuralChange<T>(entity, added: false);
#if DEBUG
        }
        finally
        {
            ExitStructuralMutation();
        }
#endif
    }

    bool TryGetDenseRow(Entity entity, out int dense)
    {
        dense = NoDenseRow;
        if (!_registry.IsAlive(entity))
            return false;

        var idx = (int)entity.Index;
        if (idx < 0 || idx >= _denseRowByEntitySlot.Length)
            return false;

        dense = _denseRowByEntitySlot[idx];
        if (dense == NoDenseRow || _rows[dense] != entity)
        {
            dense = NoDenseRow;
            return false;
        }

        return true;
    }

    void EnsureSlotMap(int minSlots)
    {
        if (_denseRowByEntitySlot.Length >= minSlots)
            return;

        var oldLen = _denseRowByEntitySlot.Length;
        var newLen = Math.Max(minSlots, oldLen < InitialValues ? InitialValues : oldLen * 2);
        Array.Resize(ref _denseRowByEntitySlot, newLen);
        for (var i = oldLen; i < newLen; i++)
            _denseRowByEntitySlot[i] = NoDenseRow;
    }

    void EnsureDenseCapacityForCount(int minLength)
    {
        if (_values.Length >= minLength)
            return;

        var newLen = _values.Length;
        while (newLen < minLength)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        GrowDenseArrays(newLen, notifyImplicitGrow: true);
    }

    void GrowDenseArrays(int newLength, bool notifyImplicitGrow)
    {
        var oldLen = _values.Length;
        if (newLength <= oldLen)
            return;

        Array.Resize(ref _values, newLength);
        Array.Resize(ref _rows, newLength);

        if (notifyImplicitGrow)
            _onCapacityGrew?.Invoke(oldLen, newLength);
    }
}
