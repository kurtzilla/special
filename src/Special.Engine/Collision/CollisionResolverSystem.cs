using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Collision;

public sealed class CollisionResolverConfig
{
    public int MaxCommandsPerTick { get; init; } = 8192;
    public bool DropHandlersWhenBudgetExceeded { get; init; } = true;
}

public readonly struct CollisionResolverMetrics
{
    public readonly int EventsDrained;
    public readonly int HandlersInvoked;
    public readonly int CommandsQueued;
    public readonly int DestroyCommandsQueued;
    public readonly int BudgetDrops;

    public CollisionResolverMetrics(
        int eventsDrained,
        int handlersInvoked,
        int commandsQueued,
        int destroyCommandsQueued,
        int budgetDrops)
    {
        EventsDrained = eventsDrained;
        HandlersInvoked = handlersInvoked;
        CommandsQueued = commandsQueued;
        DestroyCommandsQueued = destroyCommandsQueued;
        BudgetDrops = budgetDrops;
    }
}

/// <summary>
/// Consumes <see cref="CollisionEventBuffer"/> and resolves registered interactions by layer pair.
/// Resolution never mutates <see cref="Registry"/> directly; all structural changes are queued in <see cref="EntityCommandBuffer"/>.
/// </summary>
public sealed class CollisionResolverSystem : IFixedUpdateSystem
{
    public const int MaxLayers = 32;

    static readonly Type[] ReadComponents = [typeof(CollisionLayerComponent), typeof(CollisionEventsPhaseToken)];
    static readonly IReadOnlyList<Type> WriteComponents = JobAccess.EmptyWrite;

    readonly CollisionEventBuffer _events;
    readonly Action<Entity, Entity, EntityCommandBuffer>?[,] _lookupMatrix =
        new Action<Entity, Entity, EntityCommandBuffer>?[MaxLayers, MaxLayers];
    readonly List<CollisionEvent> _drainScratch = new(128);
    readonly HashSet<Entity> _pendingDestroy = new();

    Registry _registry = null!;
    ComponentPool<CollisionLayerComponent> _collisionLayers = null!;

    public CollisionResolverConfig Config { get; }
    public CollisionResolverMetrics LastMetrics { get; private set; }

    public CollisionResolverSystem(CollisionEventBuffer events, CollisionResolverConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events;
        Config = config ?? new CollisionResolverConfig();
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _collisionLayers = registry.GetPool<CollisionLayerComponent>();
    }

    /// <summary>
    /// Registers a collision resolution handler for the unordered layer pair.
    /// A-B and B-A map to the same matrix slot.
    /// </summary>
    public void RegisterHandler(
        CollisionLayer a,
        CollisionLayer b,
        Action<Entity, Entity, EntityCommandBuffer> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var aIndex = a.ToMatrixIndex();
        var bIndex = b.ToMatrixIndex();
        ValidateLayerIndex(aIndex);
        ValidateLayerIndex(bIndex);

        var row = Math.Min(aIndex, bIndex);
        var col = Math.Max(aIndex, bIndex);
        _lookupMatrix[row, col] = handler;
    }

    public void FixedUpdate(float fixedDeltaTime, EntityCommandBuffer? entityCommands)
    {
        _ = fixedDeltaTime;
        if (entityCommands is null)
            throw new InvalidOperationException("CollisionResolverSystem requires a non-null EntityCommandBuffer.");

        _pendingDestroy.Clear();
        _events.DrainTo(_drainScratch);
        var eventsDrained = _drainScratch.Count;
        var handlersInvoked = 0;
        var commandsQueued = 0;
        var destroyCommandsQueued = 0;
        var budgetDrops = 0;
        var commandBudget = Config.MaxCommandsPerTick;
        if (commandBudget < 0)
            commandBudget = 0;

        for (var i = 0; i < _drainScratch.Count; i++)
        {
            var collisionEvent = _drainScratch[i];
            var entityA = collisionEvent.EntityA;
            var entityB = collisionEvent.EntityB;

            if (_pendingDestroy.Contains(entityA) || _pendingDestroy.Contains(entityB))
                continue;

            if (!_registry.IsAlive(entityA) || !_registry.IsAlive(entityB))
                continue;

            if (!_collisionLayers.TryGet(entityA, out var layersA) || !_collisionLayers.TryGet(entityB, out var layersB))
                continue;

            var layerA = layersA.Layer.ToMatrixIndex();
            var layerB = layersB.Layer.ToMatrixIndex();

            var row = Math.Min(layerA, layerB);
            var col = Math.Max(layerA, layerB);
            var handler = _lookupMatrix[row, col];
            if (handler is null)
                continue;

            if (Config.DropHandlersWhenBudgetExceeded && commandsQueued >= commandBudget)
            {
                budgetDrops++;
                continue;
            }

            var beforeCount = entityCommands.RecordedCount;
            handlersInvoked++;

            // Keep callback ordering canonical for deterministic handler assumptions.
            if (layerA < layerB || (layerA == layerB && IsOrderedPair(entityA, entityB)))
                handler(entityA, entityB, entityCommands);
            else
                handler(entityB, entityA, entityCommands);

            var afterCount = entityCommands.RecordedCount;
            if (afterCount <= beforeCount)
                continue;

            commandsQueued += afterCount - beforeCount;
            for (var cmdIdx = beforeCount; cmdIdx < afterCount; cmdIdx++)
            {
                var cmd = entityCommands.GetRecordedCommandAt(cmdIdx);
                if (cmd.Kind != EntityCommandKind.DestroyEntity)
                    continue;

                destroyCommandsQueued++;
                _pendingDestroy.Add(cmd.Entity);
            }
        }

        _drainScratch.Clear();
        _pendingDestroy.Clear();
        LastMetrics = new CollisionResolverMetrics(
            eventsDrained,
            handlersInvoked,
            commandsQueued,
            destroyCommandsQueued,
            budgetDrops);
    }

    static bool IsOrderedPair(Entity entityA, Entity entityB)
    {
        if (entityA.Index != entityB.Index)
            return entityA.Index < entityB.Index;

        return entityA.Generation < entityB.Generation;
    }

    static void ValidateLayerIndex(int layerIndex)
    {
        if ((uint)layerIndex >= MaxLayers)
            throw new ArgumentOutOfRangeException(nameof(layerIndex), $"Layer index must be in [0, {MaxLayers - 1}].");
    }
}
