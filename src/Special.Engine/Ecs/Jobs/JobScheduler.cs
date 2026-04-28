using System.Collections.Generic;
using System.Threading.Tasks;
using Special.Engine.Ecs;

namespace Special.Engine.Ecs.Jobs;

/// <summary>
/// Resolves <see cref="IJob"/> lists into topological batches via <see cref="DependencyResolver"/>, then runs each batch with
/// <see cref="Parallel.For(System.Int32,System.Int32,ParallelOptions,System.Action{System.Int32})"/>, which schedules work on the
/// <see cref="System.Threading.ThreadPool"/> (no dedicated threads and no affinity between a particular <see cref="IJob"/> instance and an OS thread).
/// <see cref="ParallelOptions.MaxDegreeOfParallelism"/> only caps concurrency; the runtime still assigns workers dynamically.
/// </summary>
public sealed class JobScheduler
{
    readonly DependencyResolver _resolver;
    readonly List<List<IJob>> _batches = new();
    readonly ParallelOptions _parallelOptions;

    /// <param name="maxJobsPerTick">Upper bound on jobs per dispatch (pre-allocates batch slots).</param>
    /// <param name="maxDegreeOfParallelismOverride">When null, uses <see cref="Environment.ProcessorCount"/> (clamped to at least 1).</param>
    public JobScheduler(int maxJobsPerTick, int? maxDegreeOfParallelismOverride = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxJobsPerTick, 0);
        _resolver = new DependencyResolver(maxJobsPerTick);
        var dop = maxDegreeOfParallelismOverride ?? Environment.ProcessorCount;
        if (dop < 1)
            dop = 1;

        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = dop };

        for (var i = 0; i < maxJobsPerTick; i++)
            _batches.Add(new List<IJob>(16));
    }

    /// <summary>Resolves dependencies into layers, then executes each layer in parallel (no per-job command buffers).</summary>
    public void Dispatch(IReadOnlyList<IJob> jobs, in JobContext context)
    {
        DispatchCore(jobs, in context, registry: null, parallelCommandBuffers: null);
    }

    /// <summary>
    /// Like <see cref="Dispatch(IReadOnlyList{IJob},in JobContext)"/>, but clears <paramref name="parallelCommandBuffers"/>[k] before each
    /// job k, passes it as <see cref="JobContext.EntityCommands"/>, then runs <see cref="EntityCommandBuffer.Playback"/> on the
    /// <paramref name="registry"/> in job index order after the batch barrier.
    /// </summary>
    public void Dispatch(IReadOnlyList<IJob> jobs, in JobContext context, Registry registry, EntityCommandBuffer[] parallelCommandBuffers)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(parallelCommandBuffers);
        if (parallelCommandBuffers.Length < jobs.Count)
            throw new ArgumentException("Scratch array must cover every job index (length >= jobs.Count).", nameof(parallelCommandBuffers));

        DispatchCore(jobs, in context, registry, parallelCommandBuffers);
    }

    void DispatchCore(IReadOnlyList<IJob> jobs, in JobContext context, Registry? registry, EntityCommandBuffer[]? parallelCommandBuffers)
    {
        if (jobs.Count == 0)
            return;

        var batchCount = _resolver.ResolveIntoBatches(jobs, _batches);

        var template = context;
        for (var b = 0; b < batchCount; b++)
        {
            var batch = _batches[b];
            if (batch.Count == 0)
                continue;

            if (parallelCommandBuffers is not null && registry is not null)
            {
                if (parallelCommandBuffers.Length < batch.Count)
                    throw new InvalidOperationException("Parallel command buffer scratch is shorter than a resolved batch.");

                Parallel.For(0, batch.Count, _parallelOptions, k =>
                {
                    var buf = parallelCommandBuffers[k];
                    buf.Clear();
                    var ctx = new JobContext(
                        template.VariableDeltaTime,
                        template.FixedDeltaTime,
                        template.IsFixedStep,
                        buf);
                    batch[k].Execute(in ctx);
                });

                for (var k = 0; k < batch.Count; k++)
                    parallelCommandBuffers[k].Playback(registry);
            }
            else
            {
                var noEcbCtx = new JobContext(template.VariableDeltaTime, template.FixedDeltaTime, template.IsFixedStep);
                Parallel.For(0, batch.Count, _parallelOptions, k => batch[k].Execute(in noEcbCtx));
            }
        }
    }
}
