using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Ecs;

/// <summary>
/// Fixed-timestep system (physics, deterministic logic). Invoked from <see cref="EcsWorld.Tick"/> inside the capped fixed loop.
/// </summary>
public interface IFixedUpdateSystem : IJob
{
    /// <summary>Called once when registered; cache <see cref="Registry.GetPool{T}"/> results here.</summary>
    void Initialize(Registry registry);

    /// <summary>
    /// Called zero or more times per frame; <paramref name="fixedDeltaTime"/> equals <see cref="EcsWorld.FixedTimeStep"/>.
    /// When non-null, <paramref name="entityCommands"/> is this job’s <see cref="EntityCommandBuffer"/>.
    /// </summary>
    void FixedUpdate(float fixedDeltaTime, EntityCommandBuffer? entityCommands);

    /// <inheritdoc />
    void IJob.Execute(in JobContext context)
    {
        if (context.IsFixedStep)
            FixedUpdate(context.FixedDeltaTime, context.EntityCommands);
    }
}
