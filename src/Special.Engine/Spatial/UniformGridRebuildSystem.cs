using System.Numerics;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Spatial;

/// <summary>
/// Rebuilds <see cref="UniformGrid"/> on each fixed step after position writers.
/// Lifecycle contract: this rebuild happens during fixed dispatch; grid candidates must still be validated
/// with <see cref="Registry.IsAlive"/> when queried after destroy flush.
/// </summary>
public sealed class UniformGridRebuildSystem : IFixedUpdateSystem
{
    static readonly Type[] ReadComponents = [typeof(Position)];
    // Scheduling token: declares that this system refreshes UniformGrid state before grid readers run.
    static readonly Type[] WriteComponents = [typeof(UniformGrid)];

    readonly UniformGrid _grid;
    readonly UniformGridClearJob _clearJob;
    readonly UniformGridPopulateJob _populateJob;

    Registry _registry = null!;
    ComponentPool<Position> _positions = null!;
    Entity[] _entityScratch = Array.Empty<Entity>();
    Vector2[] _positionScratch = Array.Empty<Vector2>();

    public UniformGridRebuildSystem(UniformGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        _grid = grid;
        _clearJob = new UniformGridClearJob(_grid);
        _populateJob = new UniformGridPopulateJob(_grid);
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _positions = registry.GetPool<Position>();
    }

    public void FixedUpdate(float fixedDeltaTime, EntityCommandBuffer? entityCommands)
    {
        _ = fixedDeltaTime;
        _ = entityCommands;

        var count = _positions.Count;
        EnsureScratchCapacity(count);

        var kept = 0;
        for (var i = 0; i < count; i++)
        {
            var entity = _positions.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            var pos = _positions.Values[i];
            _entityScratch[kept] = entity;
            _positionScratch[kept] = new Vector2(pos.X, pos.Y);
            kept++;
        }

        _clearJob.Execute(new JobContext(0f, EcsWorld.FixedTimeStep, isFixedStep: true));
        _populateJob.Bind(_entityScratch, _positionScratch, kept);
        _populateJob.Execute(new JobContext(0f, EcsWorld.FixedTimeStep, isFixedStep: true));
    }

    void EnsureScratchCapacity(int minCapacity)
    {
        if (_entityScratch.Length >= minCapacity)
            return;

        var newLen = Math.Max(minCapacity, _entityScratch.Length == 0 ? 8 : _entityScratch.Length * 2);
        Array.Resize(ref _entityScratch, newLen);
        Array.Resize(ref _positionScratch, newLen);
    }
}
