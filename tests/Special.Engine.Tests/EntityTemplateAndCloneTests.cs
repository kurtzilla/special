using Special.Engine.Ecs;
using Special.Engine.Ecs.Components;

namespace Special.Engine.Tests;

public sealed class EntityTemplateAndCloneTests
{
    [Fact]
    public void Instantiate_applies_template_in_order()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var template = new EntityTemplate()
            .Add(new Position(1, 2))
            .Add(new Velocity(3, 4));

        var e = r.Instantiate(template);
        Assert.True(r.IsAlive(e));
        Assert.True(r.GetPool<Position>().TryGet(e, out var p));
        Assert.True(r.GetPool<Velocity>().TryGet(e, out var v));
        Assert.Equal(1f, p.X);
        Assert.Equal(2f, p.Y);
        Assert.Equal(3f, v.X);
        Assert.Equal(4f, v.Y);
    }

    [Fact]
    public void Instantiate_factory_invoked_at_apply_time()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var n = 0;
        var template = new EntityTemplate().Add(() => new Position(++n, 0));
        var a = r.Instantiate(template);
        var b = r.Instantiate(template);
        Assert.True(r.GetPool<Position>().TryGet(a, out var pa));
        Assert.True(r.GetPool<Position>().TryGet(b, out var pb));
        Assert.Equal(1f, pa.X);
        Assert.Equal(2f, pb.X);
    }

    [Fact]
    public void Clone_copies_all_present_pools_to_new_entity()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var src = world.CreateEntity();
        Assert.True(r.GetPool<Position>().TryAdd(src, new Position(10, 20)));
        Assert.True(r.GetPool<Velocity>().TryAdd(src, new Velocity(0.5f, -1f)));

        var dst = r.Clone(src);
        Assert.NotEqual(src, dst);
        Assert.True(r.GetPool<Position>().TryGet(dst, out var p));
        Assert.True(r.GetPool<Velocity>().TryGet(dst, out var v));
        Assert.Equal(10f, p.X);
        Assert.Equal(20f, p.Y);
        Assert.Equal(0.5f, v.X);
        Assert.Equal(-1f, v.Y);
    }

    [Fact]
    public void Clone_empty_source_yields_empty_destination()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var src = world.CreateEntity();
        var dst = r.Clone(src);
        Assert.False(r.GetPool<Position>().Contains(dst));
        Assert.False(r.GetPool<Velocity>().Contains(dst));
    }

    [Fact]
    public void Clone_throws_when_source_not_alive()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var e = world.CreateEntity();
        world.RequestDestroy(e);
        world.FlushDeferredDestroys();
        Assert.Throws<ArgumentException>(() => r.Clone(e));
    }

    /// <summary>Struct with a reference field to exercise optional cloner path.</summary>
    public readonly struct TagWithRef
    {
        public readonly string? Label;
        public TagWithRef(string? label) => Label = label;
    }

    [Fact]
    public void RegisterComponentCloner_used_when_struct_contains_references()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        r.RegisterComponentCloner<TagWithRef>(t => new TagWithRef(t.Label is null ? null : $"clone:{t.Label}"));

        var src = world.CreateEntity();
        Assert.True(r.GetPool<TagWithRef>().TryAdd(src, new TagWithRef("a")));

        var dst = r.Clone(src);
        Assert.True(r.GetPool<TagWithRef>().TryGet(dst, out var tag));
        Assert.Equal("clone:a", tag.Label);
    }

    [Fact]
    public void Clone_without_registered_cloner_shallow_copies_reference_fields()
    {
        var world = new EcsWorld();
        var r = world.Registry;
        var shared = "shared-label";
        var src = world.CreateEntity();
        Assert.True(r.GetPool<TagWithRef>().TryAdd(src, new TagWithRef(shared)));

        var dst = r.Clone(src);
        Assert.True(r.GetPool<TagWithRef>().TryGet(src, out var a));
        Assert.True(r.GetPool<TagWithRef>().TryGet(dst, out var b));
        Assert.Same(a.Label, b.Label);
    }
}
