namespace Special.Engine.Ecs.Components;

/// <summary>
/// Placeholder render key (layer, atlas id, or material bucket). Replace with real draw metadata later.
/// </summary>
public readonly struct SpriteTag
{
    public readonly int Key;

    public SpriteTag(int key) => Key = key;
}
