using System.Collections.Generic;

namespace Special.Engine.Ecs.Jobs;

/// <summary>
/// Builds a DAG from <see cref="IJob.ReadOnlyComponents"/> / <see cref="IJob.WriteOnlyComponents"/> for registration order <c>0..n-1</c>,
/// then partitions jobs into topological layers (each layer may run in parallel). Reuses adjacency and scratch buffers between calls.
/// </summary>
public sealed class DependencyResolver
{
    const string CollisionEventsPhaseTokenTypeName = "CollisionEventsPhaseToken";

    readonly int _maxJobs;
    readonly List<int>[] _adj;
    readonly int[] _indegree;
    readonly bool[] _removed;
    readonly List<int> _layer = new();
    int _lastResolvedBatchCount;

    public DependencyResolver(int maxJobs)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxJobs, 0);
        _maxJobs = maxJobs;
        _adj = new List<int>[maxJobs];
        for (var i = 0; i < _adj.Length; i++)
            _adj[i] = new List<int>(16);

        _indegree = new int[maxJobs];
        _removed = new bool[maxJobs];
    }

    /// <summary>Clears only batches used last call, then fills <paramref name="batches"/>[0..return-1] with parallel-safe layers.</summary>
    /// <returns>Number of populated batch lists (caller must pre-size <paramref name="batches"/> to at least this many inner lists).</returns>
    public int ResolveIntoBatches(IReadOnlyList<IJob> jobs, List<List<IJob>> batches)
    {
        for (var i = 0; i < _lastResolvedBatchCount; i++)
            batches[i].Clear();

        var n = jobs.Count;
        if (n == 0)
        {
            _lastResolvedBatchCount = 0;
            return 0;
        }

        if (n > _maxJobs)
        {
            throw new ArgumentOutOfRangeException(nameof(jobs),
                $"Job count {n} exceeds DependencyResolver capacity {_maxJobs}.");
        }

        if (batches.Count < _maxJobs)
        {
            throw new InvalidOperationException(
                "Batches list must be pre-sized to at least DependencyResolver max jobs (one list per potential layer).");
        }

        BuildGraph(jobs, n);
        var used = RunKahn(jobs, n, batches);
        _lastResolvedBatchCount = used;
        return used;
    }

    void BuildGraph(IReadOnlyList<IJob> jobs, int n)
    {
        for (var i = 0; i < n; i++)
        {
            _adj[i].Clear();
            _indegree[i] = 0;
            _removed[i] = false;
        }

        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                if (!NeedsEdgeBefore(i, j, jobs))
                    continue;

                _adj[i].Add(j);
                _indegree[j]++;
            }
        }
    }

    /// <summary>For <c>i &lt; j</c>: edge <c>i → j</c> if <c>j</c> writes a type <c>i</c> reads or writes, or <c>j</c> reads a type <c>i</c> writes.</summary>
    static bool NeedsEdgeBefore(int i, int j, IReadOnlyList<IJob> jobs)
    {
        var ji = jobs[i];
        var jj = jobs[j];

        foreach (var t in jj.WriteOnlyComponents)
        {
            if (Contains(ji.ReadOnlyComponents, t) || Contains(ji.WriteOnlyComponents, t))
                return true;
        }

        foreach (var t in jj.ReadOnlyComponents)
        {
            if (Contains(ji.WriteOnlyComponents, t))
                return true;
        }

        return false;
    }

    static bool Contains(IReadOnlyList<Type> list, Type type)
    {
        for (var k = 0; k < list.Count; k++)
        {
            if (list[k] == type)
                return true;
        }

        return false;
    }

    int RunKahn(IReadOnlyList<IJob> jobs, int n, List<List<IJob>> batches)
    {
        var scheduled = 0;
        var batchWriteIndex = 0;

        while (scheduled < n)
        {
            _layer.Clear();
            for (var i = 0; i < n; i++)
            {
                if (!_removed[i] && _indegree[i] == 0)
                    _layer.Add(i);
            }

            if (_layer.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cyclic job dependency graph: no runnable layer while jobs remain unscheduled. Check ReadOnlyComponents / WriteOnlyComponents for inconsistent ordering.");
            }

            var batch = batches[batchWriteIndex];
            batch.Clear();
            foreach (var idx in _layer)
            {
                batch.Add(jobs[idx]);
                foreach (var v in _adj[idx])
                    _indegree[v]--;

                _removed[idx] = true;
                scheduled++;
            }

#if DEBUG
            AssertNoCollisionPhaseTokenReadWriteInSameBatch(batch);
#endif
            batchWriteIndex++;
        }

        return batchWriteIndex;
    }

#if DEBUG
    static void AssertNoCollisionPhaseTokenReadWriteInSameBatch(List<IJob> batch)
    {
        var hasWriter = false;
        var hasReader = false;

        for (var i = 0; i < batch.Count; i++)
        {
            var job = batch[i];
            if (!hasWriter && ContainsTypeByName(job.WriteOnlyComponents, CollisionEventsPhaseTokenTypeName))
                hasWriter = true;

            if (!hasReader && ContainsTypeByName(job.ReadOnlyComponents, CollisionEventsPhaseTokenTypeName))
                hasReader = true;

            if (hasWriter && hasReader)
            {
                throw new InvalidOperationException(
                    "Resolved parallel batch contains both writer and reader of CollisionEventsPhaseToken. " +
                    "Collision detection and resolution must not run in the same batch.");
            }
        }
    }

    static bool ContainsTypeByName(IReadOnlyList<Type> list, string typeName)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Name == typeName)
                return true;
        }

        return false;
    }
#endif
}
