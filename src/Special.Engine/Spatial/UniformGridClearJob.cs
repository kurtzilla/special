using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Spatial;

/// <summary>Parallel clear phase for <see cref="UniformGrid"/>.</summary>
public sealed class UniformGridClearJob : IJob
{
    static readonly Type[] WriteComponentsStatic = [typeof(SpatialGridDependencyMarker)];

    readonly UniformGrid _grid;

    public UniformGridClearJob(UniformGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        _grid = grid;
    }

    public IReadOnlyList<Type> ReadOnlyComponents => JobAccess.EmptyRead;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponentsStatic;

    public void Execute(in JobContext context)
    {
        _ = context;
        _grid.ClearHeadsParallel();
    }
}
