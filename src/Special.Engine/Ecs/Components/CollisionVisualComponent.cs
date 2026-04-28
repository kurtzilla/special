namespace Special.Engine.Ecs.Components;

/// <summary>
/// Optional per-entity debug visual metadata.
/// </summary>
public readonly struct CollisionVisualComponent
{
    public readonly uint ColorRgba;
    public readonly int Layer;

    public CollisionVisualComponent(uint colorRgba, int layer)
    {
        ColorRgba = colorRgba;
        Layer = layer;
    }
}
