using System.Numerics;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;

namespace Special.Engine.Debug;

/// <summary>
/// Global facade for migrating legacy debug draw callsites.
/// </summary>
public static class DebugHelper
{
    static Registry? s_registry;
    static DebugDrawBuffer? s_drawBuffer;

    public static void BindWorld(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        s_registry = world.Registry;
    }

    public static void BindRegistry(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        s_registry = registry;
    }

    public static void BindDrawBuffer(DebugDrawBuffer drawBuffer)
    {
        ArgumentNullException.ThrowIfNull(drawBuffer);
        s_drawBuffer = drawBuffer;
    }

    public static void DrawLine(in Vector3 start, in Vector3 end, in Vector4 color, float duration = 0f)
    {
        if (s_drawBuffer is null)
            throw new InvalidOperationException("DebugHelper draw buffer is not bound. Call BindDrawBuffer first.");

        s_drawBuffer.PushLine(in start, in end, in color, duration);
    }

    public static Entity DrawLinePersistent(in Vector3 start, in Vector3 end, in Vector4 color, float duration)
    {
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "Persistent debug duration must be > 0.");

        var registry = s_registry ?? throw new InvalidOperationException("DebugHelper registry is not bound. Call BindWorld or BindRegistry first.");
        var entity = registry.CreateEntity();
        _ = registry.GetPool<PersistentDebugLineComponent>().TryAdd(entity, new PersistentDebugLineComponent(start, end, color));
        _ = registry.GetPool<DebugLifetimeComponent>().TryAdd(entity, new DebugLifetimeComponent(duration));
        return entity;
    }
}
