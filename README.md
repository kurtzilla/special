# Special

Isometric C# engine (early stage). Code lives under `src/`; tests under `tests/`. Human-facing design notes are in [`.project/README.md`](.project/README.md); agent-oriented blueprint in [`.cursor/docs/architecture.md`](.cursor/docs/architecture.md).

```bash
dotnet build Special.sln -c Release
dotnet test Special.sln -c Release
dotnet run --project src/Special.Host
```

## Collision pipeline setup (snippet)

```csharp
using Special.Engine.Collision;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Systems;
using Special.Engine.Spatial;

var world = new EcsWorld(initialPendingDestroyCapacity: 256);
var grid = new UniformGrid(originX: -256f, originY: -256f, cellSize: 4f, cellsX: 128, cellsY: 128);
var collisionEvents = new CollisionEventBuffer();
var collisionResolver = new CollisionResolverSystem(collisionEvents);

collisionResolver.RegisterHandler(CollisionLayer.Player, CollisionLayer.Bullet, (player, projectile, cmd) =>
{
    cmd.Destroy(projectile);
    // Example: cmd.Add(player, new Damage(1));
});
// Entity setup uses CollisionLayerComponent as source of truth:
// layers.TryAdd(playerEntity, new CollisionLayerComponent(CollisionLayer.Player, CollisionLayer.Enemy | CollisionLayer.Bullet));
// layers.TryAdd(bulletEntity, new CollisionLayerComponent(CollisionLayer.Bullet, CollisionLayer.Player | CollisionLayer.Enemy));

world.AddFixedUpdateSystem(new SnapshotSystem(), runFirst: true);
world.AddFixedUpdateSystem(new MovementSystem());
world.AddFixedUpdateSystem(new UniformGridRebuildSystem(grid));
world.AddFixedUpdateSystem(new CollisionSystem(grid, collisionEvents));
world.AddFixedUpdateSystem(collisionResolver);
```

## Debug visualization pipeline (snippet)

```csharp
using Special.Engine.Debug;
using Special.Engine.Ecs.Components;
using System.Numerics;

var debugDrawBuffer = new DebugDrawBuffer(initialCapacity: 4096);
var debugRenderSystem = new DebugRenderSystem(debugDrawBuffer);
var debugPersistenceSystem = new DebugPersistenceSystem(debugDrawBuffer);
var graphicsSubmissionSystem = new GraphicsSubmissionSystem(debugDrawBuffer, new RendererDebugSubmitter());

world.AddSystem(debugRenderSystem);
world.AddSystem(debugPersistenceSystem);      // post-sim persistent emission/lifetime pass
world.AddSystem(graphicsSubmissionSystem);    // final consumer pass; submits + clears buffer

DebugHelper.BindWorld(world);
DebugHelper.BindDrawBuffer(debugDrawBuffer);

// One singleton-like settings entity controls global debug categories:
var debugSettingsEntity = world.CreateEntity();
world.Registry.GetPool<DebugSettings>().TryAdd(
    debugSettingsEntity,
    new DebugSettings(
        DebugVisualCategory.Colliders |
        DebugVisualCategory.VelocityVectors |
        DebugVisualCategory.Persistent));

// Entities opt in to visual metadata:
world.Registry.GetPool<CollisionVisualComponent>().TryAdd(
    someEntity,
    new CollisionVisualComponent(colorRgba: 0xFFFF00FF, layer: 1));

// Migration helper (callable from game logic anywhere after Bind*):
DebugHelper.DrawLine(
    start: new Vector3(0f, 0f, 0f),
    end: new Vector3(1f, 0f, 0f),
    color: new Vector4(1f, 1f, 0f, 1f));

DebugHelper.DrawLinePersistent(
    start: new Vector3(0f, 0f, 0f),
    end: new Vector3(0f, 1f, 0f),
    color: new Vector4(0f, 1f, 1f, 1f),
    duration: 2f);

sealed class RendererDebugSubmitter : IDebugPrimitiveSubmitter
{
    public void SubmitToGpu(in DebugPrimitive primitive)
    {
        // Translate primitive into your renderer command list / GPU API.
    }
}
```
