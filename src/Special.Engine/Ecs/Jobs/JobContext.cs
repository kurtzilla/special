using Special.Engine.Ecs;

namespace Special.Engine.Ecs.Jobs;

/// <summary>Immutable tick parameters passed to <see cref="IJob.Execute"/>; safe to share across parallel workers.</summary>
public readonly struct JobContext
{
    public JobContext(float variableDeltaTime, float fixedDeltaTime, bool isFixedStep, EntityCommandBuffer? entityCommands = null)
    {
        VariableDeltaTime = variableDeltaTime;
        FixedDeltaTime = fixedDeltaTime;
        IsFixedStep = isFixedStep;
        EntityCommands = entityCommands;
    }

    public float VariableDeltaTime { get; }
    public float FixedDeltaTime { get; }
    public bool IsFixedStep { get; }

    /// <summary>
    /// When non-null, parallel jobs record structural ECS changes here; applied in <see cref="JobScheduler"/> after each batch.
    /// </summary>
    public EntityCommandBuffer? EntityCommands { get; }
}
