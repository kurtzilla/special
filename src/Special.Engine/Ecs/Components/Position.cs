namespace Special.Engine.Ecs.Components;

/// <summary>
/// World-space or logical-plane position (2D). Data only—systems apply movement and isometric projection.
/// </summary>
public readonly struct Position
{
    public readonly float X;
    public readonly float Y;

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}
