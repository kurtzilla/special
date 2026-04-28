namespace Special.Engine.Ecs.Components;

/// <summary>
/// Circle collider data used for broad/narrow collision detection.
/// </summary>
public readonly struct Collider
{
    public readonly float Radius;
    public readonly int LayerMask;

    public Collider(float radius, int layerMask)
    {
        Radius = radius;
        LayerMask = layerMask;
    }
}
