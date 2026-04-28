namespace Special.Engine.Ecs;

/// <summary>
/// Opaque entity handle: packed index (20 bits) + generation (12 bits). No logic here—only identity.
/// Live handles are minted by <see cref="Registry"/>; default(<see cref="Entity"/>) is invalid.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    public const int IndexBitCount = 20;
    public const int GenerationBitCount = 12;

    public const uint MaxIndex = (1u << IndexBitCount) - 1;
    public const uint MaxGeneration = (1u << GenerationBitCount) - 1;

    readonly uint _raw;

    internal Entity(uint raw) => _raw = raw;

    /// <summary>Slot index in registries / SoA arrays (0 .. <see cref="MaxIndex"/>).</summary>
    public uint Index => _raw & MaxIndex;

    /// <summary>Version for stale-handle detection; 0 means invalid / never issued.</summary>
    public uint Generation => _raw >> IndexBitCount;

    /// <summary>True if non-zero packed value (registry still uses generation ≥ 1 for live entities).</summary>
    public bool IsValid => _raw != 0;

    internal static Entity FromParts(uint index, uint generation)
    {
        var i = index & MaxIndex;
        var g = generation & MaxGeneration;
        return new Entity((g << IndexBitCount) | i);
    }

    public bool Equals(Entity other) => _raw == other._raw;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => unchecked((int)_raw);

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public override string ToString() => $"Entity({Index}:{Generation})";
}
