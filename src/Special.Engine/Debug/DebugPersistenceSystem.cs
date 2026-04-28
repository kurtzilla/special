using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Debug;

/// <summary>
/// Emits entity-backed persistent debug lines while lifetime remains, then destroys expired entities.
/// </summary>
public sealed class DebugPersistenceSystem : IUpdateSystem
{
    static readonly Type[] ReadComponents =
    [
        typeof(PersistentDebugLineComponent),
        typeof(DebugSettings),
    ];

    static readonly Type[] WriteComponents =
    [
        typeof(DebugLifetimeComponent),
    ];

    readonly DebugDrawBuffer _drawBuffer;

    Registry _registry = null!;
    ComponentPool<PersistentDebugLineComponent> _lines = null!;
    ComponentPool<DebugLifetimeComponent> _lifetimes = null!;
    ComponentPool<DebugSettings> _debugSettings = null!;

    public DebugPersistenceSystem(DebugDrawBuffer drawBuffer)
    {
        ArgumentNullException.ThrowIfNull(drawBuffer);
        _drawBuffer = drawBuffer;
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _lines = registry.GetPool<PersistentDebugLineComponent>();
        _lifetimes = registry.GetPool<DebugLifetimeComponent>();
        _debugSettings = registry.GetPool<DebugSettings>();
    }

    public void Update(float deltaTime, EntityCommandBuffer? entityCommands)
    {
        if (!IsPersistentDebugEnabled())
            return;

        _drawBuffer.EnsureCapacity(_drawBuffer.Count + _lifetimes.Count);
        var writer = _drawBuffer.GetParallelWriter();
        var count = _lifetimes.Count;
        for (var i = 0; i < count; i++)
        {
            var entity = _lifetimes.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            if (!_lines.TryGet(entity, out var line))
                continue;

            ref var lifetime = ref _lifetimes.GetRef(entity);
            lifetime.RemainingTime -= deltaTime;

            if (lifetime.RemainingTime > 0f)
            {
                var primitive = DebugPrimitive.CreateLine(in line.Start, in line.End, in line.Color, lifetime.RemainingTime);
                writer.AddNoResize(in primitive);
                continue;
            }

            entityCommands?.Destroy(entity);
        }
    }

    bool IsPersistentDebugEnabled()
    {
        var count = _debugSettings.Count;
        for (var i = 0; i < count; i++)
        {
            var entity = _debugSettings.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            var settings = _debugSettings.Values[i];
            return settings.IsEnabled(DebugVisualCategory.Persistent);
        }

        return true;
    }
}
