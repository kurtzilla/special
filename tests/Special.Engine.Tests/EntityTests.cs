using Special.Engine.Ecs;

namespace Special.Engine.Tests;

public sealed class EntityTests
{
    [Fact]
    public void FromParts_round_trips_index_and_generation()
    {
        var e = Entity.FromParts(42, 7);
        Assert.Equal(42u, e.Index);
        Assert.Equal(7u, e.Generation);
        Assert.True(e.IsValid);
    }

    [Fact]
    public void FromParts_masks_index_and_generation_to_bit_widths()
    {
        var e = Entity.FromParts(Entity.MaxIndex, Entity.MaxGeneration);
        Assert.Equal(Entity.MaxIndex, e.Index);
        Assert.Equal(Entity.MaxGeneration, e.Generation);
    }

    [Fact]
    public void Default_is_invalid_for_identity()
    {
        var e = default(Entity);
        Assert.False(e.IsValid);
    }

    [Fact]
    public void Equality_uses_packed_raw()
    {
        var a = Entity.FromParts(1, 2);
        var b = Entity.FromParts(1, 2);
        var c = Entity.FromParts(1, 3);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
    }
}
