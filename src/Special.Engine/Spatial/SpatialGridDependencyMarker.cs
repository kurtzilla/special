namespace Special.Engine.Spatial;

/// <summary>
/// Marker-only dependency type for the job resolver to enforce clear-before-populate ordering for spatial jobs.
/// This is metadata only; no pool should be created for this type.
/// </summary>
public readonly struct SpatialGridDependencyMarker
{
}
