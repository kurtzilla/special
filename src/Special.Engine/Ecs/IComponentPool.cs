namespace Special.Engine.Ecs;

/// <summary>
/// Type-erased component storage; invoked during deferred flush before slot recycle.
/// </summary>
public interface IComponentPool
{
    /// <summary>
    /// Playback path for <see cref="EntityCommandBuffer"/>: unbox to this pool’s component type and add if the entity is alive and has no row.
    /// Returns <see langword="false"/> when the boxed value does not match the pool type or <see cref="ComponentPool{T}.TryAdd"/> fails.
    /// </summary>
    bool TryAddFromObject(Entity entity, object boxedComponent);

    /// <summary>Removes this entity's row from the pool if present; no-op if missing or handle is stale.</summary>
    void RemoveForEntityIfPresent(Entity entity);

    /// <summary>
    /// If <paramref name="source"/> has a row, copies its value to <paramref name="destination"/>.
    /// If the destination already has this component type, it is removed first so the copy matches the source.
    /// Returns <see langword="false"/> when the source has no row (missing component or stale handle).
    /// </summary>
    bool TryCloneFromTo(Entity source, Entity destination);
}
