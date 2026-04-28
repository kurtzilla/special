using Special.Engine.Loop;

namespace Special.Engine.Tests;

public sealed class GameLoopTests
{
    sealed class Spy : IGameLoopCallbacks
    {
        public int FixedCount;
        public int FrameCount;
        public ulong LastFixedFrameIndex;
        public ulong LastRenderFrameIndex;

        public void OnFixedStep(in GameTime fixedTime)
        {
            FixedCount++;
            LastFixedFrameIndex = fixedTime.FrameIndex;
        }

        public void OnFrame(in GameTime frameTime)
        {
            FrameCount++;
            LastRenderFrameIndex = frameTime.FrameIndex;
        }
    }

    [Fact]
    public void Advance_respects_max_fixed_steps_per_frame()
    {
        var spy = new Spy();
        var loop = new GameLoop(fixedDeltaTime: 0.1f, maxFixedStepsPerFrame: 3, spy);

        loop.Advance(realDeltaTime: 1f);

        Assert.Equal(3, spy.FixedCount);
        Assert.Equal(1, spy.FrameCount);
        Assert.Equal(3u, spy.LastFixedFrameIndex);
        Assert.Equal(1u, spy.LastRenderFrameIndex);
    }

    [Fact]
    public void Advance_invokes_OnFrame_every_call()
    {
        var spy = new Spy();
        var loop = new GameLoop(1f / 60f, maxFixedStepsPerFrame: 8, spy);

        loop.Advance(0.001f);
        loop.Advance(0.001f);

        Assert.Equal(2, spy.FrameCount);
    }
}
