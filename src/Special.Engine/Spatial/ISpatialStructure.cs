using System.Collections.Generic;
using System.Numerics;
using Special.Engine.Ecs;

namespace Special.Engine.Spatial;

/// <summary>
/// Broad-phase spatial index over 2D positions. Implementations should avoid per-query allocations when callers reuse buffers.
/// </summary>
public interface ISpatialStructure
{
    /// <summary>
    /// Rebuilds the structure from aligned spans of entities and positions. Entries are paired by index.
    /// </summary>
    void Update(ReadOnlySpan<Entity> entities, ReadOnlySpan<Vector2> positions);

    /// <summary>
    /// Appends candidate entities from cells touched by the circle AABB. Callers must validate candidates
    /// (e.g. <c>registry.IsAlive(entity)</c> and distance checks) before expensive collision logic.
    /// </summary>
    void Query(Vector2 center, float radius, List<Entity> results);
}
