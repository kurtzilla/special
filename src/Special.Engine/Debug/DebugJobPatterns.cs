using System.Threading.Tasks;
using Special.Engine.Ecs.Components;
using System.Numerics;

namespace Special.Engine.Debug;

/// <summary>
/// Example pattern for parallel debug emission jobs using DebugDrawBuffer parallel writer.
/// </summary>
public static class DebugJobPatterns
{
    public readonly struct EmitVelocityDebugJob
    {
        public readonly Position[] Positions;
        public readonly Velocity[] Velocities;
        public readonly NativeList<DebugPrimitive>.ParallelWriter DebugWriter;
        public readonly Vector4 Color;
        public readonly float Scale;
        public readonly float Duration;

        public EmitVelocityDebugJob(
            Position[] positions,
            Velocity[] velocities,
            NativeList<DebugPrimitive>.ParallelWriter debugWriter,
            Vector4 color,
            float scale,
            float duration)
        {
            Positions = positions;
            Velocities = velocities;
            DebugWriter = debugWriter;
            Color = color;
            Scale = scale;
            Duration = duration;
        }

        public void Execute(int index)
        {
            if ((uint)index >= (uint)Positions.Length || (uint)index >= (uint)Velocities.Length)
                return;

            var p = Positions[index];
            var v = Velocities[index];
            var start = new Vector3(p.X, p.Y, 0f);
            var end = new Vector3(p.X + v.X * Scale, p.Y + v.Y * Scale, 0f);
            var primitive = DebugPrimitive.CreateLine(
                in start,
                in end,
                in Color,
                Duration);
            DebugWriter.AddNoResize(primitive);
        }
    }

    public static void RunVelocityJob(EmitVelocityDebugJob job, int count)
    {
        Parallel.For(0, count, i => job.Execute(i));
    }
}
