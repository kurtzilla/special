using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;
using Special.Engine.Ecs.Query;

namespace Special.Engine.Ecs.Systems;

/// <summary>
/// Template system: uses <see cref="Registry.Query{Position,Velocity}"/> for entities with both components; integrates velocity into <see cref="Position"/> on the fixed step.
/// </summary>
public sealed class MovementSystem : IFixedUpdateSystem
{
    static readonly Type[] ReadComponents = [typeof(Velocity)];
    static readonly Type[] WriteComponents = [typeof(Position)];

    Query<Position, Velocity> _pvQuery = default!;

    /// <inheritdoc />
    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;

    /// <inheritdoc />
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    /// <inheritdoc />
    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _pvQuery = registry.Query<Position, Velocity>();
    }

    /// <inheritdoc />
    public void FixedUpdate(float dt, EntityCommandBuffer? entityCommands)
    {
        var step = dt > 0f ? dt : EcsWorld.FixedTimeStep;
        foreach (var row in _pvQuery)
        {
            ref readonly var velocity = ref row.Component2;
            ref var position = ref row.Component1;
            position = new Position(
                position.X + velocity.X * step,
                position.Y + velocity.Y * step);
        }
    }
}
