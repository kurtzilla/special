using Special.Engine.Ecs.Jobs;

namespace Special.Engine.Ecs;

/// <summary>
/// Owns the <see cref="Registry"/>, <see cref="CommandBuffer"/>, per-job <see cref="EntityCommandBuffer"/> scratch (parallel recording),
/// variable <see cref="IUpdateSystem"/> and fixed <see cref="IFixedUpdateSystem"/> lists, <see cref="JobScheduler"/>, and deferred entity teardown.
/// </summary>
public sealed class EcsWorld
{
    const int DefaultInitialPendingCapacity = 64;
    const int MaxFixedStepsPerTick = 5;
    const int MaxSchedulerJobs = 128;

    readonly Registry _registry = new();
    readonly CommandBuffer _commands;
    readonly EntityCommandBuffer[] _parallelCommandBuffers = new EntityCommandBuffer[MaxSchedulerJobs];
    readonly List<IUpdateSystem> _updateSystems = new();
    readonly List<IFixedUpdateSystem> _fixedUpdateSystems = new();
    readonly JobScheduler _scheduler = new(MaxSchedulerJobs);
    readonly List<IJob> _updateJobScratch = new(MaxSchedulerJobs);
    readonly List<IJob> _fixedJobScratch = new(MaxSchedulerJobs);

    float _accumulator;

    public EcsWorld(int initialPendingDestroyCapacity = DefaultInitialPendingCapacity)
    {
        _commands = new CommandBuffer(initialPendingDestroyCapacity);
        for (var i = 0; i < _parallelCommandBuffers.Length; i++)
            _parallelCommandBuffers[i] = new EntityCommandBuffer();

        ComponentCloneRegistration.RegisterReferenceComponentCloners(_registry);
    }

    /// <summary>Fixed delta for each simulated step (seconds).</summary>
    public const float FixedTimeStep = 0.02f;

    /// <summary>Fraction in <c>[0, 1]</c> between the last fixed state and the next; use with double-buffered transforms for render interpolation.</summary>
    public float InterpolationAlpha { get; private set; }

    /// <summary>Entity slots, generations, free-index stack, and component pools.</summary>
    public Registry Registry => _registry;

    /// <summary>Deferred destroy queue (main thread).</summary>
    public CommandBuffer Commands => _commands;

    /// <summary>
    /// One <see cref="EntityCommandBuffer"/> per job index for the scheduler (<see cref="JobScheduler.Dispatch"/> passes <c>[k]</c> to batch slot <c>k</c>).
    /// Not for direct mutation outside <see cref="Tick"/> unless you own the synchronization story.
    /// </summary>
    public ReadOnlySpan<EntityCommandBuffer> ParallelCommandBuffers => _parallelCommandBuffers;

    public Entity CreateEntity() => _registry.CreateEntity();

    /// <summary>Queue destroy; entity stays alive until <see cref="FlushDeferredDestroys"/>.</summary>
    public void RequestDestroy(Entity entity) => _commands.RequestDestroy(entity);

    /// <summary>Grow the command buffer so at least <paramref name="minCapacity"/> queued destroys fit.</summary>
    public void EnsurePendingDestroyCapacity(int minCapacity) => _commands.EnsureCapacity(minCapacity);

    /// <summary>Strip all pools then recycle slots for every queued destroy (end of fixed step / frame).</summary>
    public void FlushDeferredDestroys() => _commands.Flush(_registry);

    /// <summary>
    /// Registers a system that implements <see cref="IUpdateSystem"/> and/or <see cref="IFixedUpdateSystem"/>.
    /// <see cref="Initialize"/> runs at most once (update branch first if both apply). Appends to the fixed list in registration order.
    /// </summary>
    public void AddSystem<T>(T system) where T : class
    {
        ArgumentNullException.ThrowIfNull(system);
        if (system is not IUpdateSystem && system is not IFixedUpdateSystem)
            throw new ArgumentException("System must implement IUpdateSystem and/or IFixedUpdateSystem.", nameof(system));

        if (system is IUpdateSystem u)
            u.Initialize(_registry);
        else if (system is IFixedUpdateSystem f)
            f.Initialize(_registry);

        if (system is IUpdateSystem u2)
            _updateSystems.Add(u2);
        if (system is IFixedUpdateSystem f2)
            _fixedUpdateSystems.Add(f2);
    }

    /// <summary>
    /// Registers a fixed system, calls <see cref="IFixedUpdateSystem.Initialize"/>, and inserts at the front of the fixed list when <paramref name="runFirst"/> is true (e.g. snapshot before physics).
    /// </summary>
    public void AddFixedUpdateSystem(IFixedUpdateSystem system, bool runFirst = false)
    {
        ArgumentNullException.ThrowIfNull(system);
        system.Initialize(_registry);
        if (runFirst)
            _fixedUpdateSystems.Insert(0, system);
        else
            _fixedUpdateSystems.Add(system);
    }

    /// <summary>
    /// Variable updates (via <see cref="JobScheduler"/> with per-job <see cref="EntityCommandBuffer"/> and post-batch playback on <see cref="Registry"/>),
    /// capped fixed-step loop with the same for fixed jobs, updates <see cref="InterpolationAlpha"/>, then <see cref="FlushDeferredDestroys"/>.
    /// Scratch job lists are cleared and reused each tick (no per-frame scheduling allocations once capacities stabilize).
    /// </summary>
    public void Tick(float deltaTime)
    {
        _updateJobScratch.Clear();
        if (_updateJobScratch.Capacity < _updateSystems.Count)
            _updateJobScratch.Capacity = _updateSystems.Count;
        for (var i = 0; i < _updateSystems.Count; i++)
            _updateJobScratch.Add(_updateSystems[i]);

        var updateCtx = new JobContext(deltaTime, FixedTimeStep, isFixedStep: false);
        _scheduler.Dispatch(_updateJobScratch, in updateCtx, _registry, _parallelCommandBuffers);

        _accumulator += deltaTime;

        var steps = 0;
        while (steps < MaxFixedStepsPerTick && _accumulator >= FixedTimeStep)
        {
            _fixedJobScratch.Clear();
            if (_fixedJobScratch.Capacity < _fixedUpdateSystems.Count)
                _fixedJobScratch.Capacity = _fixedUpdateSystems.Count;
            for (var j = 0; j < _fixedUpdateSystems.Count; j++)
                _fixedJobScratch.Add(_fixedUpdateSystems[j]);

            var fixedCtx = new JobContext(deltaTime, FixedTimeStep, isFixedStep: true);
            _scheduler.Dispatch(_fixedJobScratch, in fixedCtx, _registry, _parallelCommandBuffers);

            _accumulator -= FixedTimeStep;
            steps++;
        }

        InterpolationAlpha = _accumulator / FixedTimeStep;

        FlushDeferredDestroys();
    }
}
