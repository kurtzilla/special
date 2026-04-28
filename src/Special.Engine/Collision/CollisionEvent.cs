using System.Numerics;
using Special.Engine.Ecs;

namespace Special.Engine.Collision;

/// <summary>
/// Pairwise collision hit reported by the detection pipeline.
/// </summary>
public readonly struct CollisionEvent
{
    public readonly Entity EntityA;
    public readonly Entity EntityB;
    public readonly Vector2 ContactPoint;

    public CollisionEvent(Entity entityA, Entity entityB, Vector2 contactPoint)
    {
        EntityA = entityA;
        EntityB = entityB;
        ContactPoint = contactPoint;
    }
}
