namespace Special.Engine.Debug;

[Flags]
public enum DebugVisualCategory
{
    None = 0,
    Colliders = 1 << 0,
    Grid = 1 << 1,
    Velocity = 1 << 2,
    VelocityVectors = 1 << 3,
    Persistent = 1 << 4,
}
