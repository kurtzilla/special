using System.Numerics;

namespace Special.Engine.Ecs.Components;

/// <summary>
/// Data-only persistent debug line payload.
/// </summary>
public readonly struct PersistentDebugLineComponent
{
    public readonly Vector3 Start;
    public readonly Vector3 End;
    public readonly Vector4 Color;

    public PersistentDebugLineComponent(Vector3 start, Vector3 end, Vector4 color)
    {
        Start = start;
        End = end;
        Color = color;
    }
}
