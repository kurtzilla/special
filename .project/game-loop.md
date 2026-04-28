# Game loop and time

## Purpose

`Special.Engine.Loop.GameLoop` separates **fixed simulation** steps from **variable frame** work (rendering, interpolation) without depending on a graphics API or ECS `World`.

## Types

- **`GameTime`** — `DeltaTime`, `TotalElapsed`, `FrameIndex` (ordinal for that callback stream).
- **`IGameLoopCallbacks`** — host implements `OnFixedStep` and `OnFrame` once (no per-frame `Action` allocations from the loop itself).
- **`GameLoop`** — accumulator pattern: each `Advance(realDelta)` may run **up to `MaxFixedStepsPerFrame`** fixed steps, then always runs **one** `OnFrame` with the **real** wall delta.

Fixed steps use `DeltaTime == FixedDeltaTime` and `TotalElapsed` as **simulation** time. Frame steps use the real delta and **wall** `TotalElapsed`.

## Snippet: host stub

```csharp
sealed class Host : IGameLoopCallbacks
{
    public void OnFixedStep(in GameTime t) { /* simulation */ }
    public void OnFrame(in GameTime t) { /* draw / interpolate */ }
}

var loop = new GameLoop(fixedDeltaTime: 1f / 60f, maxFixedStepsPerFrame: 8, new Host());
loop.Advance(realDeltaTime: 1f / 60f);
```

## Notes

- **Spiral of death:** `MaxFixedStepsPerFrame` caps how many fixed steps run per wall frame when the sim falls behind.
- **ECS wiring:** `GameLoop` does not reference `Entity`. Prefer **`EcsWorld.Tick(deltaTime)`** once per frame: it runs variable and fixed systems, then **`FlushDeferredDestroys()`**. If you do not use **`Tick`**, call **`FlushDeferredDestroys()`** after your own system pass so queued destroys apply before the next step or network serialize.
