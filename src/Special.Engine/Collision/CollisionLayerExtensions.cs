using System.Numerics;

namespace Special.Engine.Collision;

public static class CollisionLayerExtensions
{
    /// <summary>
    /// Converts a single-bit layer value (1,2,4,8,...) to zero-based matrix index (0,1,2,3,...).
    /// </summary>
    public static int ToMatrixIndex(this CollisionLayer layer)
    {
        var bits = (uint)layer;
        if (bits == 0 || (bits & (bits - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(layer), "Layer must be a single-bit CollisionLayer value.");

        return BitOperations.TrailingZeroCount(bits);
    }
}
