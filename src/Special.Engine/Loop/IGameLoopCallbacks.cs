namespace Special.Engine.Loop;

/// <summary>
/// Host implements this once; <see cref="GameLoop"/> invokes callbacks without per-frame delegate allocations.
/// </summary>
public interface IGameLoopCallbacks
{
    /// <summary>Deterministic simulation step at fixed delta.</summary>
    void OnFixedStep(in GameTime fixedTime);

    /// <summary>Variable frame step (render / interpolation) using the real wall delta for this display frame.</summary>
    void OnFrame(in GameTime frameTime);
}
