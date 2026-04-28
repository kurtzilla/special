using Special.Engine.Ecs.Components;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Ecs.Systems;

/// <summary>
/// At the start of each fixed step, copies <see cref="Transform.CurrentPosition"/> into <see cref="Transform.PreviousPosition"/>
/// so render code can interpolate with <c>MathUtils.Interpolate</c> using <see cref="EcsWorld.InterpolationAlpha"/>.
/// Register with <see cref="EcsWorld.AddFixedUpdateSystem"/> and <c>runFirst: true</c> so it runs before physics.
/// </summary>
public sealed class SnapshotSystem : IFixedUpdateSystem
{
    static readonly Type[] ReadComponents = [];
    static readonly Type[] WriteComponents = [typeof(Transform)];

    ComponentPool<Transform> _transforms = null!;

    /// <inheritdoc />
    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;

    /// <inheritdoc />
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    /// <inheritdoc />
    public void Initialize(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _transforms = registry.GetPool<Transform>();
    }

    /// <inheritdoc />
    public void FixedUpdate(float fixedDeltaTime, EntityCommandBuffer? entityCommands)
    {
        for (var i = 0; i < _transforms.Count; i++)
        {
            ref var t = ref _transforms.GetRefAtDense(i);
            t.PreviousPosition = t.CurrentPosition;
        }
    }
}
