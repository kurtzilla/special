namespace Special.Engine.Loop;

/// <summary>
/// Time context for a single tick. Immutable value type—safe to pass by read-only reference.
/// </summary>
public readonly struct GameTime
{
    /// <summary>Delta for this step (fixed delta for <see cref="IGameLoopCallbacks.OnFixedStep"/>; wall delta for <see cref="IGameLoopCallbacks.OnFrame"/>).</summary>
    public float DeltaTime { get; init; }

    /// <summary>Monotonic elapsed time in the same clock as <see cref="DeltaTime"/> (simulation time for fixed steps; wall time for frame steps).</summary>
    public float TotalElapsed { get; init; }

    /// <summary>Ordinal for this callback kind (fixed-step count vs render-frame count).</summary>
    public ulong FrameIndex { get; init; }
}
