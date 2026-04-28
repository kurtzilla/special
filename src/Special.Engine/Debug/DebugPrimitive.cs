using System.Numerics;

namespace Special.Engine.Debug;

public enum PrimitiveType : byte
{
    Line = 0,
    Circle = 1,
}

/// <summary>
/// Blittable debug primitive payload for frame-local debug draw emission.
/// Circle uses Start as center and End.X as radius.
/// </summary>
public struct DebugPrimitive
{
    public PrimitiveType Type;
    public Vector3 Start;
    public Vector3 End;
    public Vector4 Color;
    public float Duration;

    public static DebugPrimitive CreateLine(
        in Vector3 start,
        in Vector3 end,
        in Vector4 color,
        float duration = 0f)
    {
        return new DebugPrimitive
        {
            Type = PrimitiveType.Line,
            Start = start,
            End = end,
            Color = color,
            Duration = duration,
        };
    }

    public static DebugPrimitive CreateCircle(
        in Vector3 center,
        float radius,
        in Vector4 color,
        float duration = 0f)
    {
        return new DebugPrimitive
        {
            Type = PrimitiveType.Circle,
            Start = center,
            End = new Vector3(radius, 0f, 0f),
            Color = color,
            Duration = duration,
        };
    }
}
