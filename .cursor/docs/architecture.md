# Engine Blueprint: Isometric C# Engine

## Current Status

- Architecture: DOD (Data Oriented Design)
- Target: High-performance, moddable, server-authoritative

## Component Pattern (The ECS/DOD Hybrid)

- **Entity:** A simple `int` ID (Handle).
- **Component:** A `readonly struct` containing only data (e.g., `Position`, `Velocity`, `SpriteRenderer`).
- **System:** A static class or struct that processes arrays of components.

## Isometric Rendering Rules

- **Coordinate System:** [Define your system, e.g., X,Y screen coordinates vs. Isometric depth].
- **Sorting:** Entities must be sorted by a `Depth` value calculated from their screen Y-position before draw calls.
- **Batching:** Systems must group draw calls by Texture/Material to minimize state changes.

## Modding Policy

- **Interface-First:** Every game system must implement an interface (e.g., `ISystem`).
- **Loading:** Mods must load via `AssemblyLoadContext` to allow dynamic unloading.
- **Exposed API:** Only interfaces marked with `[PublicAPI]` attribute are safe for modders to access.

## Multiplayer Policy

- **Server-Authoritative:** The Client sends `InputPackets`. The Server computes the state and broadcasts `StateSnapshots`.
- **Interpolation:** The Client must implement visual interpolation between the last two received snapshots.
