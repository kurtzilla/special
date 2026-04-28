using Special.Engine.Ecs;

namespace Special.Engine.Tests;

public sealed class RegistryTests
{
    [Fact]
    public void Create_destroy_reuse_bumps_generation()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var a = world.CreateEntity();
        Assert.True(r.IsAlive(a));
        world.RequestDestroy(a);
        world.FlushDeferredDestroys();
        Assert.False(r.IsAlive(a));

        var b = world.CreateEntity();
        Assert.True(r.IsAlive(b));
        Assert.Equal(a.Index, b.Index);
        Assert.NotEqual(a.Generation, b.Generation);
    }

    [Fact]
    public void Distinct_slots_have_independent_generations()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        Assert.NotEqual(a.Index, b.Index);
        Assert.True(r.IsAlive(a));
        Assert.True(r.IsAlive(b));
    }
}
