using Special.Engine.Ecs.Components;

namespace Special.Engine.Tests;

public sealed class ComponentUnmanagedTests
{
    static void AssertUnmanaged<T>() where T : unmanaged
    {
    }

    [Fact]
    public void Example_components_are_unmanaged()
    {
        AssertUnmanaged<Position>();
        AssertUnmanaged<Velocity>();
        AssertUnmanaged<SpriteTag>();
        AssertUnmanaged<Transform>();
    }
}
