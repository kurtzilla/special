using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;

namespace Special.Engine.Tests;

public sealed class DeferredDestroyTests
{
    [Fact]
    public void Flush_clears_component_then_recycles_slot()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var positions = r.GetPool<Position>();
        var e = world.CreateEntity();
        Assert.True(positions.TryAdd(e, new Position(1, 2)));

        world.RequestDestroy(e);
        Assert.True(r.IsAlive(e));
        Assert.True(positions.Contains(e));

        world.FlushDeferredDestroys();

        Assert.False(r.IsAlive(e));
        Assert.False(positions.Contains(e));
        Assert.Equal(0, positions.Count);
    }

    [Fact]
    public void CommandBuffer_Flush_rejects_null_registry()
    {
        var buffer = new CommandBuffer();
        Assert.Throws<ArgumentNullException>(() => buffer.Flush(null!));
    }

    [Fact]
    public void Empty_flush_is_no_op()
    {
        var r = new Registry();
        var buffer = new CommandBuffer();
        buffer.Flush(r);
        var e = r.CreateEntity();
        Assert.True(r.IsAlive(e));
    }

    [Fact]
    public void Duplicate_RequestDestroy_same_entity_flush_once()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var e = world.CreateEntity();
        world.RequestDestroy(e);
        world.RequestDestroy(e);
        Assert.Equal(2, world.Commands.PendingCount);

        world.FlushDeferredDestroys();

        Assert.False(r.IsAlive(e));
        Assert.Equal(0, world.Commands.PendingCount);
    }

    [Fact]
    public void EnsurePendingDestroyCapacity_allows_many_queued_without_throw()
    {
        const int n = 32;
        var world = new EcsWorld(initialPendingDestroyCapacity: n);
        world.EnsurePendingDestroyCapacity(n);
        var r = world.Registry;
        var entities = new Entity[n];
        for (var i = 0; i < n; i++)
            entities[i] = world.CreateEntity();

        foreach (var e in entities)
            world.RequestDestroy(e);

        world.FlushDeferredDestroys();

        foreach (var e in entities)
            Assert.False(r.IsAlive(e));
    }

    [Fact]
    public void Stale_default_entity_in_queue_flush_skips()
    {
        var world = new EcsWorld();
        var buffer = world.Commands;
        buffer.RequestDestroy(default);
        world.FlushDeferredDestroys();
        Assert.Equal(0, buffer.PendingCount);
    }

    [Fact]
    public void Stale_never_minted_handle_flush_skips()
    {
        var world = new EcsWorld();
        var buffer = world.Commands;
        buffer.RequestDestroy(Entity.FromParts(999, 1));
        world.FlushDeferredDestroys();
        Assert.Equal(0, buffer.PendingCount);
    }

    [Fact]
    public void RequestDestroy_flush_RequestDestroy_same_stale_second_flush_skips()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var e = world.CreateEntity();
        world.RequestDestroy(e);
        world.FlushDeferredDestroys();
        Assert.False(r.IsAlive(e));

        world.RequestDestroy(e);
        world.FlushDeferredDestroys();
        Assert.Equal(0, world.Commands.PendingCount);
    }

    [Fact]
    public void PendingCount_tracks_queue_until_flush()
    {
        var world = new EcsWorld();
        world.RequestDestroy(world.CreateEntity());
        world.RequestDestroy(world.CreateEntity());
        Assert.Equal(2, world.Commands.PendingCount);
        world.FlushDeferredDestroys();
        Assert.Equal(0, world.Commands.PendingCount);
    }
}
