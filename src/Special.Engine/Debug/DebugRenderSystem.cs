using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;
using System.Numerics;

namespace Special.Engine.Debug;

/// <summary>
/// Read-only ECS pass that emits debug primitives into <see cref="DebugDrawBuffer"/>.
/// Rendering code consumes that buffer later in frame.
/// </summary>
public sealed class DebugRenderSystem : IUpdateSystem
{
    static readonly Type[] ReadComponents =
    [
        typeof(Position),
        typeof(Velocity),
        typeof(Collider),
        typeof(CollisionVisualComponent),
        typeof(DebugSettings),
    ];

    static readonly IReadOnlyList<Type> WriteComponents = JobAccess.EmptyWrite;
    static readonly Vector4 DefaultColliderColor = ColorFromRgba(0xFF00FFFF);
    static readonly Vector4 DefaultVelocityColor = ColorFromRgba(0xFF00FF00);

    readonly DebugDrawBuffer _drawBuffer;

    Registry _registry = null!;
    ComponentPool<Position> _positions = null!;
    ComponentPool<Velocity> _velocities = null!;
    ComponentPool<Collider> _colliders = null!;
    ComponentPool<CollisionVisualComponent> _collisionVisuals = null!;
    ComponentPool<DebugSettings> _debugSettings = null!;

    public DebugRenderSystem(DebugDrawBuffer drawBuffer)
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
        _positions = registry.GetPool<Position>();
        _velocities = registry.GetPool<Velocity>();
        _colliders = registry.GetPool<Collider>();
        _collisionVisuals = registry.GetPool<CollisionVisualComponent>();
        _debugSettings = registry.GetPool<DebugSettings>();
    }

    public void Update(float deltaTime, EntityCommandBuffer? entityCommands)
    {
        _ = deltaTime;
        _ = entityCommands;

        if (!TryGetSettings(out var settings))
            return;

        if (settings.IsEnabled(DebugVisualCategory.Colliders))
            EmitColliders();

        if (settings.IsEnabled(DebugVisualCategory.VelocityVectors) || settings.IsEnabled(DebugVisualCategory.Velocity))
            EmitVelocityVectors();

        if (settings.IsEnabled(DebugVisualCategory.Grid))
            EmitGrid();
    }

    bool TryGetSettings(out DebugSettings settings)
    {
        var count = _debugSettings.Count;
        for (var i = 0; i < count; i++)
        {
            var entity = _debugSettings.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            settings = _debugSettings.Values[i];
            return true;
        }

        settings = default;
        return false;
    }

    void EmitColliders()
    {
        _drawBuffer.EnsureCapacity(_drawBuffer.Count + _colliders.Count);
        var writer = _drawBuffer.GetParallelWriter();
        var count = _colliders.Count;
        for (var i = 0; i < count; i++)
        {
            var entity = _colliders.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            var collider = _colliders.Values[i];
            if (!_positions.TryGet(entity, out var position))
                continue;

            if (!_collisionVisuals.TryGet(entity, out var visual))
                continue;

            var radius = collider.Radius < 0f ? 0f : collider.Radius;
            var color = visual.ColorRgba == 0 ? DefaultColliderColor : ColorFromRgba(visual.ColorRgba);
            var center = new Vector3(position.X, position.Y, 0f);
            var primitive = DebugPrimitive.CreateCircle(in center, radius, in color);
            writer.AddNoResize(in primitive);
        }
    }

    void EmitVelocityVectors()
    {
        _drawBuffer.EnsureCapacity(_drawBuffer.Count + _velocities.Count);
        var writer = _drawBuffer.GetParallelWriter();
        var count = _velocities.Count;
        for (var i = 0; i < count; i++)
        {
            var entity = _velocities.Entities[i];
            if (!_registry.IsAlive(entity))
                continue;

            if (!_positions.TryGet(entity, out var position))
                continue;

            if (!_velocities.TryGet(entity, out var velocity))
                continue;

            var color = DefaultVelocityColor;
            if (_collisionVisuals.TryGet(entity, out var visual) && visual.ColorRgba != 0)
                color = ColorFromRgba(visual.ColorRgba);

            var start = new Vector3(position.X, position.Y, 0f);
            var end = new Vector3(position.X + velocity.X, position.Y + velocity.Y, 0f);
            var primitive = DebugPrimitive.CreateLine(in start, in end, in color);
            writer.AddNoResize(in primitive);
        }
    }

    void EmitGrid()
    {
        // Grid emission is host/project specific (grid dimensions, origin, and projection mode).
    }

    static Vector4 ColorFromRgba(uint rgba)
    {
        var r = (rgba >> 24) & 0xFF;
        var g = (rgba >> 16) & 0xFF;
        var b = (rgba >> 8) & 0xFF;
        var a = rgba & 0xFF;
        return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
    }
}
