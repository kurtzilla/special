using System.Numerics;

namespace Special.Engine.Ecs.Components;

/// <summary>
/// Double-buffered world position for render interpolation: <see cref="CurrentPosition"/> is advanced by simulation;
/// <see cref="PreviousPosition"/> holds the last fixed snapshot (see <see cref="Systems.SnapshotSystem"/>).
/// On spawn, set <see cref="PreviousPosition"/> equal to <see cref="CurrentPosition"/> once to avoid a first-frame pop.
/// </summary>
public struct Transform
{
    public Vector3 CurrentPosition;
    public Vector3 PreviousPosition;
}
