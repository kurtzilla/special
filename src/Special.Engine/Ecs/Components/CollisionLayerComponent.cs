using Special.Engine.Collision;

namespace Special.Engine.Ecs.Components;

/// <summary>
/// Collision filtering data: own layer plus allowed collision mask.
/// </summary>
public readonly struct CollisionLayerComponent
{
    public readonly CollisionLayer Layer;
    public readonly CollisionLayer Mask;

    public CollisionLayerComponent(CollisionLayer layer, CollisionLayer mask)
    {
        Layer = layer;
        Mask = mask;
    }
}
