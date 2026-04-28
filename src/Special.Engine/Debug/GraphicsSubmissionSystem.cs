using Special.Engine.Ecs;
using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Debug;

/// <summary>
/// Consumer pass that reads debug primitives and forwards them to a renderer submission boundary.
/// </summary>
public sealed class GraphicsSubmissionSystem : IUpdateSystem
{
    static readonly IReadOnlyList<Type> ReadComponents = JobAccess.EmptyRead;
    static readonly IReadOnlyList<Type> WriteComponents = JobAccess.EmptyWrite;

    readonly DebugDrawBuffer _drawBuffer;
    readonly IDebugPrimitiveSubmitter _submitter;

    public GraphicsSubmissionSystem(DebugDrawBuffer drawBuffer, IDebugPrimitiveSubmitter submitter)
    {
        ArgumentNullException.ThrowIfNull(drawBuffer);
        ArgumentNullException.ThrowIfNull(submitter);
        _drawBuffer = drawBuffer;
        _submitter = submitter;
    }

    public IReadOnlyList<Type> ReadOnlyComponents => ReadComponents;
    public IReadOnlyList<Type> WriteOnlyComponents => WriteComponents;

    public void Initialize(Registry registry)
    {
        _ = registry;
    }

    public void Update(float deltaTime, EntityCommandBuffer? entityCommands)
    {
        _ = deltaTime;
        _ = entityCommands;

        var primitives = _drawBuffer.AsArray();
        for (var i = 0; i < primitives.Length; i++)
            _submitter.SubmitToGpu(in primitives[i]);

        _drawBuffer.Clear();
    }
}
