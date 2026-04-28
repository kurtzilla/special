using Special.Engine.Ecs;
using Special.Engine.Loop;
using Special.Host.Platform;

namespace Special.Host;

/// <summary>
/// Bridges game loop callbacks to host-owned ECS simulation and presentation.
/// </summary>
public sealed class HostCallbacks : IGameLoopCallbacks
{
    readonly EcsWorld _world;
    readonly IHostWindow _window;
    readonly Action<float> _present;
    float _fpsSampleElapsed;
    int _fpsSampleFrames;

    public HostCallbacks(EcsWorld world, IHostWindow window, Action<float> present)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(present);

        _world = world;
        _window = window;
        _present = present;
    }

    public void OnFixedStep(in GameTime fixedTime)
    {
        // Fixed hook is intentionally reserved for host-side deterministic events.
        _ = fixedTime;
    }

    public void OnFrame(in GameTime frameTime)
    {
        _world.Tick(frameTime.DeltaTime);
        TrackFps(frameTime.DeltaTime);
        _present(frameTime.DeltaTime);
    }

    void TrackFps(float deltaTime)
    {
        _fpsSampleElapsed += deltaTime;
        _fpsSampleFrames++;

        if (_fpsSampleElapsed < 1f)
            return;

        var fps = _fpsSampleFrames / _fpsSampleElapsed;
        _window.SetTitle($"Special Host - FPS {fps:F1}");
        _fpsSampleElapsed = 0f;
        _fpsSampleFrames = 0;
    }
}
