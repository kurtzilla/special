# ECS: entities and components

## Goals

- **Data-oriented:** simulation data in value types and **dense SoA** arrays for cache-friendly iteration.
- **Stale-safe handles:** recycling a slot bumps **generation** so old `Entity` values do not alias new occupants.
- **Zero-allocation hot paths:** stores use raw arrays and integer indices; free-slot recycling uses **`int[]` + stack count** (LIFO), not **`List<int>`**.

## Entity

`Special.Engine.Ecs.Entity` is a **readonly struct** packing:

- **20-bit index** — slot id into the registry and into per-store reverse maps.
- **12-bit generation** — incremented when a slot is destroyed and reused.

`default(Entity)` is invalid (`IsValid` is false). Live handles are created via **`Registry.CreateEntity()`** or **`EcsWorld.CreateEntity()`** (delegates to the world’s registry).

## Registry

**`Registry`** holds:

- **Slot data:** `Entity` mint/recycle, **`IsAlive`**, **`SlotCount`**.
- **Free indices:** LIFO **stack** implemented as **`int[]` + count** (pop on create when non-empty, push on destroy)—not `List<int>`.
- **Pools:** **`private Dictionary<Type, IComponentPool>`** and **`GetPool<T>()`** to get or create **`ComponentPool<T>`** (`T : struct`). Systems should **cache** the returned pool at initialization.

**`Registry.Destroy`** is **`internal`** (only **`CommandBuffer.Flush`** calls it after **`RemoveFromAllPools`**). It only bumps generation and returns the index to the free stack; it **does not** remove component rows from pools by itself.

**`RemoveFromAllPools(Entity)`** (internal / flush-only) iterates every registered pool and calls **`RemoveForEntityIfPresent`**. It is invoked only from **`CommandBuffer.Flush(Registry)`**, not from **`RequestDestroy`**.

## Generic component pool (`ComponentPool<T>`)

One pool per component type, **`T : struct`**, implements **`IComponentPool`**:

- **`Values`** / **`Entities`** — parallel spans of length **`Count`** for hot loops.
- **`TryAdd` / `Remove` / `TryGet` / `Contains` / `GetRef`** — keyed by **`Entity`** (O(1) via slot index map); swap-with-last removal.
- **`Resize(minimumCapacity)`** — grow dense **`T[]`** / parallel entity rows before load (avoids implicit growth during simulation). Optional ctor callback **`(oldLength, newLength)`** runs only on **implicit** dense-array growth (e.g. from **`TryAdd`** when the buffer was too small).

Obtain via **`world.Registry.GetPool<Position>()`** (or hold a **`Registry`** reference), not `new ComponentPool` unless tests.

## CommandBuffer

- **`RequestDestroy(Entity)`** — append to internal **`Entity[]`** queue (grow with **`Array.Resize`** when needed).
- **`EnsureCapacity`**, **`Flush(Registry)`** — flush walks the queue; for each alive entity calls **`registry.RemoveFromAllPools`** then **`registry.Destroy`**, then clears the count.

## EcsWorld

- Owns **`Registry Registry`**, **`CommandBuffer Commands`**, **`IUpdateSystem`** and **`IFixedUpdateSystem`** lists (no `Entities` façade).
- **`Tick(deltaTime)`** — variable **`Update`**, fixed loop (**`FixedTimeStep`**, max 5 steps per frame) **`FixedUpdate`**, **`InterpolationAlpha`**, then **`FlushDeferredDestroys`** (single-frame pipeline).
- **`AddSystem`** — register systems implementing **`IUpdateSystem`** and/or **`IFixedUpdateSystem`** (shared **`Initialize`** once). **`AddFixedUpdateSystem(system, runFirst)`** prepends fixed systems (e.g. **`SnapshotSystem`** before physics).
- **`RequestDestroy`** → **`Commands.RequestDestroy`**. **`EnsurePendingDestroyCapacity`** → **`Commands.EnsureCapacity`**. **`FlushDeferredDestroys`** → **`Commands.Flush(Registry)`** (also invoked at end of **`Tick`**).

There is **no** `RegisterStore` list; all pools live on the registry dictionary.

## Spatial uniform grid

- **`ISpatialStructure`** — broad-phase contract: **`Update(ReadOnlySpan<Entity>, ReadOnlySpan<Vector2>)`** and **`Query(Vector2, float, List<Entity>)`**.
- **`Special.Engine.Spatial.UniformGrid`** — finite 2D grid over **`Position`** using a linked-list-in-arrays layout (`Head[]` + `Next[]`) and atomic head insert (`Interlocked.Exchange`) during parallel populate. No per-cell `List<Entity>` allocations.
- **`UniformGridRebuildSystem`** — **`IFixedUpdateSystem`** that reads **`Position`**, copies live rows into reusable scratch arrays, then rebuilds the grid each fixed step. Register after **`MovementSystem`** (or any fixed writer of **`Position`**).
- **Query validity rule** — query returns candidates only; consumers must call **`registry.IsAlive(entity)`** before collision/narrow-phase checks, since destroy flush occurs after fixed dispatch.

### Snippet: fixed-step collision pipeline registration (illustrative)

```csharp
using Special.Engine.Collision;
using Special.Engine.Ecs;
using Special.Engine.Ecs.Systems;
using Special.Engine.Spatial;

var world = new EcsWorld(initialPendingDestroyCapacity: 256);

var grid = new UniformGrid(
    originX: -256f,
    originY: -256f,
    cellSize: 4f,
    cellsX: 128,
    cellsY: 128);
var collisionEvents = new CollisionEventBuffer();
var collisionResolver = new CollisionResolverSystem(collisionEvents);

collisionResolver.RegisterHandler(CollisionLayer.Player, CollisionLayer.Bullet, (player, projectile, cmd) =>
{
    cmd.Destroy(projectile);
    // Example: cmd.Add(player, new Damage(1));
});
// Entity setup uses CollisionLayerComponent as source of truth:
// layerPool.TryAdd(playerEntity, new CollisionLayerComponent(CollisionLayer.Player, CollisionLayer.Enemy | CollisionLayer.Bullet));
// layerPool.TryAdd(bulletEntity, new CollisionLayerComponent(CollisionLayer.Bullet, CollisionLayer.Player | CollisionLayer.Enemy));

world.AddFixedUpdateSystem(new SnapshotSystem(), runFirst: true);
world.AddFixedUpdateSystem(new MovementSystem());
world.AddFixedUpdateSystem(new UniformGridRebuildSystem(grid));
world.AddFixedUpdateSystem(new CollisionSystem(grid, collisionEvents));
world.AddFixedUpdateSystem(collisionResolver);
```

## Components (examples)

Under `Special.Engine.Ecs.Components`:

- **`Position`** / **`Velocity`** — `float` X/Y in a shared 2D logical space.
- **`Transform`** — **`System.Numerics.Vector3`** **`CurrentPosition`** / **`PreviousPosition`** for render interpolation (see **`SnapshotSystem`** + **`MathUtils.Interpolate`**).
- **`SpriteTag`** — placeholder `int` key for batching/material selection later.

Examples are **`unmanaged`** (`Transform` is a mutable **`struct`** for SoA writes).

### Snippet: world + store (illustrative)

```csharp
var world = new EcsWorld(initialPendingDestroyCapacity: 256);
world.EnsurePendingDestroyCapacity(512);
var positions = world.Registry.GetPool<Position>();

var e = world.CreateEntity();
positions.TryAdd(e, new Position(1, 2));

world.RequestDestroy(e);
world.FlushDeferredDestroys();
```
