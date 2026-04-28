namespace Special.Engine.Loop;

/// <summary>
/// Fixed-timestep accumulator with a per-frame cap; one variable <see cref="IGameLoopCallbacks.OnFrame"/> per wall tick.
/// Does not reference ECS types—host wires simulation inside callbacks.
/// </summary>
public sealed class GameLoop
{
    readonly IGameLoopCallbacks _callbacks;
    readonly float _fixedDeltaTime;
    readonly int _maxFixedStepsPerFrame;
    float _accumulator;
    float _simulationElapsed;
    float _wallElapsed;
    ulong _fixedFrameOrdinal;
    ulong _renderFrameOrdinal;

    public float FixedDeltaTime => _fixedDeltaTime;

    public int MaxFixedStepsPerFrame => _maxFixedStepsPerFrame;

    public GameLoop(float fixedDeltaTime, int maxFixedStepsPerFrame, IGameLoopCallbacks callbacks)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fixedDeltaTime, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFixedStepsPerFrame, 1);
        _fixedDeltaTime = fixedDeltaTime;
        _maxFixedStepsPerFrame = maxFixedStepsPerFrame;
        _callbacks = callbacks;
    }

    /// <summary>Advance by a wall-clock delta (seconds).</summary>
    public void Advance(float realDeltaTime)
    {
        if (realDeltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(realDeltaTime));

        _accumulator += realDeltaTime;
        var steps = 0;
        while (_accumulator >= _fixedDeltaTime && steps < _maxFixedStepsPerFrame)
        {
            _accumulator -= _fixedDeltaTime;
            steps++;
            _simulationElapsed += _fixedDeltaTime;
            _fixedFrameOrdinal++;
            _callbacks.OnFixedStep(new GameTime
            {
                DeltaTime = _fixedDeltaTime,
                TotalElapsed = _simulationElapsed,
                FrameIndex = _fixedFrameOrdinal,
            });
        }

        _wallElapsed += realDeltaTime;
        _renderFrameOrdinal++;
        _callbacks.OnFrame(new GameTime
        {
            DeltaTime = realDeltaTime,
            TotalElapsed = _wallElapsed,
            FrameIndex = _renderFrameOrdinal,
        });
    }
}
