namespace Special.Engine.Ecs.Components;

/// <summary>
/// Remaining lifetime in seconds for a persistent debug primitive entity.
/// </summary>
public struct DebugLifetimeComponent
{
    public float RemainingTime;

    public DebugLifetimeComponent(float remainingTime)
    {
        RemainingTime = remainingTime;
    }
}
