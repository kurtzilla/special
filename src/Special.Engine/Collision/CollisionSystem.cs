using System.Numerics;
using System.Threading.Tasks;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;
using Special.Engine.Spatial;

namespace Special.Engine.Collision;

/// <summary>
/// Detects circle collisions and reports events. Resolution is intentionally handled by downstream systems.
/// </summary>
public sealed class CollisionSystem : IFixedUpdateSystem
{
    static readonly Type[] ReadComponents = [typeof(Position), typeof(Collider), typeof(CollisionLayerComponent), typeof(UniformGrid)];
    static readonly Type[] WriteComponents = [typeof(CollisionEventsPhaseToken)];

    readonly UniformGrid _grid;
    readonly CollisionEventBuffer _events;

    Registry _registry = null!;
    ComponentPool<Position> _positions = null!;
    ComponentPool<Collider> _colliders = null!;
    ComponentPool<CollisionLayerComponent> _collisionLayers = null!;

    public CollisionSystem(UniformGrid grid, CollisionEventBuffer events)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(events);
        _grid = grid;
        _events = events;
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _positions = registry.GetPool<Position>();
        _colliders = registry.GetPool<Collider>();
        _collisionLayers = registry.GetPool<CollisionLayerComponent>();
    }

    public void FixedUpdate(float fixedDeltaTime, EntityCommandBuffer? entityCommands)
    {
        _ = fixedDeltaTime;
        _ = entityCommands;

        _events.Clear();

        var count = _colliders.Count;
        if (count == 0)
            return;

        using var candidateScratch = new ThreadLocal<List<Entity>>(() => new List<Entity>(32), trackAllValues: false);

        Parallel.For(0, count, i =>
        {
            var candidates = candidateScratch.Value!;
            candidates.Clear();

            var entityA = _colliders.Entities[i];
            if (!_registry.IsAlive(entityA))
                return;

            var colliderA = _colliders.Values[i];
            if (!_collisionLayers.TryGet(entityA, out var layersA))
                return;

            if (!_positions.TryGet(entityA, out var positionA))
                return;

            var radiusA = colliderA.Radius;
            if (radiusA < 0f)
                radiusA = 0f;

            var centerA = new Vector2(positionA.X, positionA.Y);
            _grid.Query(centerA, radiusA, candidates);

            for (var c = 0; c < candidates.Count; c++)
            {
                var entityB = candidates[c];
                if (!IsOrderedPair(entityA, entityB))
                    continue;

                if (!_registry.IsAlive(entityB))
                    continue;

                if (!_colliders.TryGet(entityB, out var colliderB) ||
                    !_positions.TryGet(entityB, out var positionB) ||
                    !_collisionLayers.TryGet(entityB, out var layersB))
                {
                    continue;
                }

                if ((layersA.Mask & layersB.Layer) == 0)
                    continue;

                if (!IntersectsCircleCircle(positionA, colliderA.Radius, positionB, colliderB.Radius))
                    continue;

                var midpoint = new Vector2(
                    (positionA.X + positionB.X) * 0.5f,
                    (positionA.Y + positionB.Y) * 0.5f);
                _events.Add(new CollisionEvent(entityA, entityB, midpoint));
            }
        });
    }

    static bool IntersectsCircleCircle(in Position a, float radiusA, in Position b, float radiusB)
    {
        var safeRadiusA = radiusA < 0f ? 0f : radiusA;
        var safeRadiusB = radiusB < 0f ? 0f : radiusB;
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var distSq = (dx * dx) + (dy * dy);
        var sum = safeRadiusA + safeRadiusB;
        return distSq <= (sum * sum);
    }

    static bool IsOrderedPair(Entity entityA, Entity entityB)
    {
        if (entityA.Index != entityB.Index)
            return entityA.Index < entityB.Index;

        return entityA.Generation < entityB.Generation;
    }
}
