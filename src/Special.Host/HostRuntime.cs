using Special.Engine.Ecs;
using Special.Engine.Ecs.Systems;
using Special.Engine.Loop;
using Special.Host.Platform;
using System.Diagnostics;

namespace Special.Host;

public sealed class HostRuntime : IDisposable
{
    readonly IHostWindow _window;
    readonly GameLoop _gameLoop;
    readonly Stopwatch _frameClock;
    bool _disposed;

    HostRuntime(IHostWindow window, GameLoop gameLoop)
    {
        _window = window;
        _gameLoop = gameLoop;
        _frameClock = Stopwatch.StartNew();
    }

    public static HostRuntime CreateDefault()
    {
        var world = new EcsWorld(initialPendingDestroyCapacity: 256);
        world.AddFixedUpdateSystem(new SnapshotSystem(), runFirst: true);
        world.AddFixedUpdateSystem(new MovementSystem());

        var window = new WinFormsHostWindow("Special Host", width: 1280, height: 720);
        var callbacks = new HostCallbacks(world, window, window.Present);
        var gameLoop = new GameLoop(
            fixedDeltaTime: EcsWorld.FixedTimeStep,
            maxFixedStepsPerFrame: 5,
            callbacks);

        return new HostRuntime(window, gameLoop);
    }

    public void Run()
    {
        _window.Show();

        while (_window.IsOpen)
        {
            _window.PumpEvents();
            var dt = (float)_frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();
            _gameLoop.Advance(dt);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _window.Dispose();
        _disposed = true;
    }
}
