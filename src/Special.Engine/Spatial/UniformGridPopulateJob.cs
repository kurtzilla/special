using System.Numerics;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Spatial;

/// <summary>
/// Parallel populate phase for <see cref="UniformGrid"/>. Reads bound arrays prepared by the owning rebuild system.
/// </summary>
public sealed class UniformGridPopulateJob : IJob
{
    static readonly Type[] ReadComponentsStatic = [typeof(SpatialGridDependencyMarker), typeof(Position)];

    readonly UniformGrid _grid;
    Entity[] _entities = Array.Empty<Entity>();
    Vector2[] _positions = Array.Empty<Vector2>();
    int _count;

    public UniformGridPopulateJob(UniformGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        _grid = grid;
    }

    /// <summary>Binds array-backed data for the next execute call.</summary>
    public void Bind(Entity[] entities, Vector2[] positions, int count)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > entities.Length || count > positions.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not exceed bound arrays length.");

        _entities = entities;
        _positions = positions;
        _count = count;
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponentsStatic;
    public IReadOnlyList<Type> WriteOnlyComponents => JobAccess.EmptyWrite;

    public void Execute(in JobContext context)
    {
        _ = context;
        _grid.PopulateParallel(_entities.AsSpan(0, _count), _positions.AsSpan(0, _count));
    }
}
