namespace Special.Engine.Ecs.Components;

/// <summary>
/// Linear velocity in the same space as <see cref="Position"/>. Data only.
/// </summary>
public readonly struct Velocity
{
    public readonly float X;
    public readonly float Y;

    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}
