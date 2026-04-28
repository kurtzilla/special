using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Ecs;

/// <summary>
/// Variable-rate system (input, camera, visuals). Invoked from <see cref="EcsWorld.Tick"/> before fixed simulation.
/// </summary>
public interface IUpdateSystem : IJob
{
    /// <summary>Called once when registered; cache <see cref="Registry.GetPool{T}"/> results here.</summary>
    void Initialize(Registry registry);

    /// <summary>
    /// Called once per frame with the variable frame delta. When non-null, <paramref name="entityCommands"/> is this job’s
    /// <see cref="EntityCommandBuffer"/> (structural changes are applied after the parallel batch).
    /// </summary>
    void Update(float deltaTime, EntityCommandBuffer? entityCommands);

    /// <inheritdoc />
    void IJob.Execute(in JobContext context)
    {
        if (!context.IsFixedStep)
            Update(context.VariableDeltaTime, context.EntityCommands);
    }
}
