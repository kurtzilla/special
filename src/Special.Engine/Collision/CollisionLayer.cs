namespace Special.Engine.Collision;

/// <summary>
/// Collision layers used for filtering and resolver dispatch.
/// Matrix lookup assumes each entity resolves with a single effective layer (first set bit).
/// </summary>
[Flags]
public enum CollisionLayer
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    Bullet = 1 << 2,
    Wall = 1 << 3,
    Trigger = 1 << 4,
}
