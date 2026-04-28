using System.Numerics;
using Special.Engine.Ecs.Components;

namespace Special.Engine;

/// <summary>Interpolation helpers; static methods only.</summary>
public static class MathUtils
{
    /// <summary>Linear interpolation between the last fixed snapshot and the current simulated position.</summary>
    public static Vector3 Interpolate(in Transform transform, float alpha) =>
        Vector3.Lerp(transform.PreviousPosition, transform.CurrentPosition, alpha);

    /// <summary>
    /// Writes interpolated positions into <paramref name="destination"/>; requires <c>destination.Length &gt;= transforms.Length</c>.
    /// Caller supplies backing storage; no allocations inside this method.
    /// </summary>
    public static void Interpolate(ReadOnlySpan<Transform> transforms, Span<Vector3> destination, float alpha)
    {
        if (destination.Length < transforms.Length)
            throw new ArgumentException("Destination span must be at least as long as transforms.", nameof(destination));

        for (var i = 0; i < transforms.Length; i++)
            destination[i] = Interpolate(in transforms[i], alpha);
    }
}
