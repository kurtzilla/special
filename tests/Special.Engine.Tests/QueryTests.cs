using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;

namespace Special.Engine.Tests;

public sealed class QueryTests
{
    [Fact]
    public void Match_set_updates_when_second_component_added_after_first()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Position, Velocity>();

        var e = world.CreateEntity();
        Assert.True(r.GetPool<Position>().TryAdd(e, new Position(1, 2)));
        Assert.Equal(0, q.Count);

        Assert.True(r.GetPool<Velocity>().TryAdd(e, new Velocity(3, 4)));
        Assert.Equal(1, q.Count);
        Assert.Equal(e, q.Entities[0]);
    }

    [Fact]
    public void Match_set_updates_when_first_component_added_after_second()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Velocity, Position>();

        var e = world.CreateEntity();
        Assert.True(r.GetPool<Velocity>().TryAdd(e, new Velocity(1, 2)));
        Assert.Equal(0, q.Count);

        Assert.True(r.GetPool<Position>().TryAdd(e, new Position(5, 6)));
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void Query_registered_after_both_components_rebuilds_from_pools()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var e = world.CreateEntity();
        Assert.True(r.GetPool<Position>().TryAdd(e, new Position(10, 20)));
        Assert.True(r.GetPool<Velocity>().TryAdd(e, new Velocity(1, 1)));

        var q = r.Query<Position, Velocity>();
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void Remove_one_component_removes_from_query()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Position, Velocity>();
        var e = world.CreateEntity();
        r.GetPool<Position>().TryAdd(e, default);
        r.GetPool<Velocity>().TryAdd(e, default);
        Assert.Equal(1, q.Count);

        r.GetPool<Position>().Remove(e);
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void Flush_destroy_removes_entity_from_query()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Position, Velocity>();
        var e = world.CreateEntity();
        r.GetPool<Position>().TryAdd(e, default);
        r.GetPool<Velocity>().TryAdd(e, default);
        Assert.Equal(1, q.Count);

        world.RequestDestroy(e);
        world.FlushDeferredDestroys();
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void RefreshSpans_aligns_values_with_entities()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Position, Velocity>();
        var e = world.CreateEntity();
        r.GetPool<Position>().TryAdd(e, new Position(2, 3));
        r.GetPool<Velocity>().TryAdd(e, new Velocity(4, 5));

        var spans = q.RefreshSpans();
        Assert.Equal(1, spans.Values1.Length);
        Assert.Equal(1, spans.Values2.Length);
        Assert.Equal(2f, spans.Values1[0].X);
        Assert.Equal(3f, spans.Values1[0].Y);
        Assert.Equal(4f, spans.Values2[0].X);
        Assert.Equal(5f, spans.Values2[0].Y);
    }

    [Fact]
    public void Iterator_exposes_refs_into_pools()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var q = r.Query<Position, Velocity>();
        var e = world.CreateEntity();
        r.GetPool<Position>().TryAdd(e, new Position(0, 0));
        r.GetPool<Velocity>().TryAdd(e, new Velocity(10, 20));

        foreach (var row in q)
        {
            Assert.Equal(e, row.Entity);
            row.Component1 = new Position(1, 1);
            row.Component2 = new Velocity(2, 2);
        }

        Assert.True(r.GetPool<Position>().TryGet(e, out var p2));
        Assert.True(r.GetPool<Velocity>().TryGet(e, out var v2));
        Assert.Equal(1f, p2.X);
        Assert.Equal(2f, v2.X);
    }
}
