namespace Special.Engine.Debug;

/// <summary>
/// Renderer-facing submission boundary for debug primitives.
/// </summary>
public interface IDebugPrimitiveSubmitter
{
    void SubmitToGpu(in DebugPrimitive primitive);
}
