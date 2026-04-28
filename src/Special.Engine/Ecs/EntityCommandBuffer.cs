namespace Special.Engine.Ecs;

/// <summary>Kinds of structural operations deferred to <see cref="EntityCommandBuffer.Playback"/>.</summary>
public enum EntityCommandKind : byte
{
    AddComponent,
    RemoveComponent,
    DestroyEntity,
}

/// <summary>One recorded ECS mutation applied in order during <see cref="EntityCommandBuffer.Playback"/>.</summary>
public readonly struct EntityCommand
{
    public EntityCommandKind Kind { get; }
    public Entity Entity { get; }
    /// <summary>Set for <see cref="EntityCommandKind.AddComponent"/> and <see cref="EntityCommandKind.RemoveComponent"/>.</summary>
    public Type? ComponentType { get; }
    /// <summary>Boxed struct value for <see cref="EntityCommandKind.AddComponent"/> only; <see cref="Add{T}"/> allocates a box.</summary>
    public object? Payload { get; }

    public EntityCommand(EntityCommandKind kind, Entity entity, Type? componentType = null, object? payload = null)
    {
        Kind = kind;
        Entity = entity;
        ComponentType = componentType;
        Payload = payload;
    }
}

/// <summary>
/// Per parallel job scratch list of structural commands. Each parallel worker writes its own instance (no locks).
/// <see cref="Playback"/> is the only place that mutates <see cref="Registry"/> pools and slots from these commands.
/// </summary>
public sealed class EntityCommandBuffer
{
    const int DefaultListCapacity = 32;

    readonly List<EntityCommand> _commands = new(DefaultListCapacity);

    /// <summary>Number of queued commands (before <see cref="Playback"/> or <see cref="Clear"/>).</summary>
    public int RecordedCount => _commands.Count;

    /// <summary>Returns a previously recorded command by index for diagnostics/analysis.</summary>
    public EntityCommand GetRecordedCommandAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        if (index >= _commands.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _commands[index];
    }

    /// <summary>Drops queued commands without applying them.</summary>
    public void Clear() => _commands.Clear();

    /// <summary>Queue an add; boxing allocates. Playback calls <see cref="ComponentPool{T}.TryAdd"/>.</summary>
    public void Add<T>(Entity entity, in T component) where T : struct
    {
        _commands.Add(new EntityCommand(EntityCommandKind.AddComponent, entity, typeof(T), component));
    }

    /// <summary>Queue a component removal for this type if a pool exists at playback time.</summary>
    public void Remove<T>(Entity entity) where T : struct
    {
        _commands.Add(new EntityCommand(EntityCommandKind.RemoveComponent, entity, typeof(T)));
    }

    /// <summary>Queue full entity teardown (strip all pools, recycle slot), same semantics as deferred destroy flush.</summary>
    public void Destroy(Entity entity)
    {
        _commands.Add(new EntityCommand(EntityCommandKind.DestroyEntity, entity));
    }

    /// <summary>Apply all commands in order, then clear. Call only from the main thread after parallel jobs in a batch complete.</summary>
    public void Playback(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        for (var i = 0; i < _commands.Count; i++)
        {
            var cmd = _commands[i];
            switch (cmd.Kind)
            {
                case EntityCommandKind.AddComponent:
                    if (!registry.IsAlive(cmd.Entity) || cmd.ComponentType is null || cmd.Payload is null)
                        break;

                    var addPool = registry.GetOrCreatePool(cmd.ComponentType);
                    _ = addPool.TryAddFromObject(cmd.Entity, cmd.Payload);
                    break;

                case EntityCommandKind.RemoveComponent:
                    if (cmd.ComponentType is null)
                        break;

                    if (registry.TryGetPool(cmd.ComponentType, out var removePool))
                        removePool.RemoveForEntityIfPresent(cmd.Entity);

                    break;

                case EntityCommandKind.DestroyEntity:
                    if (!registry.IsAlive(cmd.Entity))
                        break;

                    registry.RemoveFromAllPools(cmd.Entity);
                    registry.Destroy(cmd.Entity);
                    break;
            }
        }

        _commands.Clear();
    }
}
