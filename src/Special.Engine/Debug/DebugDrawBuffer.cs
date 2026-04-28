using System.Numerics;

namespace Special.Engine.Debug;

/// <summary>
/// Shared debug primitive buffer. Simulation/debug systems produce into it, renderer consumes later in frame.
/// </summary>
public sealed class DebugDrawBuffer : IDisposable
{
    readonly NativeList<DebugPrimitive> _primitives;

    public DebugDrawBuffer(int initialCapacity = 1024)
    {
        _primitives = new NativeList<DebugPrimitive>(initialCapacity);
    }

    public int Count => _primitives.Length;

    public void EnsureCapacity(int minCapacity) => _primitives.EnsureCapacity(minCapacity);

    public NativeList<DebugPrimitive>.ParallelWriter GetParallelWriter() => _primitives.AsParallelWriter();

    public NativeList<DebugPrimitive>.ParallelWriter AsParallelWriter() => GetParallelWriter();

    public ReadOnlySpan<DebugPrimitive> AsArray() => _primitives.AsArray();

    public void PushLine(in Vector3 start, in Vector3 end, in Vector4 color, float duration = 0f)
        => _primitives.Add(DebugPrimitive.CreateLine(in start, in end, in color, duration));

    public void PushCircle(in Vector3 center, float radius, in Vector4 color, float duration = 0f)
        => _primitives.Add(DebugPrimitive.CreateCircle(in center, radius, in color, duration));

    /// <summary>
    /// Resets count to zero while retaining allocated memory.
    /// Call at frame start or immediately after renderer consume.
    /// </summary>
    public void Clear() => _primitives.Clear();

    public void Dispose() => _primitives.Dispose();
}
