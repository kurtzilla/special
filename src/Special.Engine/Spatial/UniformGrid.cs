using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Special.Engine.Ecs;

namespace Special.Engine.Spatial;

/// <summary>
/// Axis-aligned finite uniform grid using a linked-list-in-arrays layout:
/// <c>Head[cell]</c> points to the first span index in that cell, and <c>Next[index]</c>
/// links to the next span index in the same cell (or -1).
/// </summary>
public sealed class UniformGrid : ISpatialStructure
{
    const int Empty = -1;

    readonly float _originX;
    readonly float _originY;
    readonly float _cellSize;
    readonly float _invCellSize;
    readonly int _cellsX;
    readonly int _cellsY;
    readonly int _numCells;

    int[] _head;
    int[] _next;
    Entity[] _entityBySpanIndex;
    Vector2[] _positionBySpanIndex;
    int _activeCount;

    public float OriginX => _originX;
    public float OriginY => _originY;
    public float CellSize => _cellSize;
    public int CellsX => _cellsX;
    public int CellsY => _cellsY;
    public int CellCount => _numCells;

    /// <summary>Per-cell chain head index; -1 means empty.</summary>
    public ReadOnlySpan<int> Head => _head.AsSpan(0, _numCells);

    /// <summary>Next pointer per active span index.</summary>
    public ReadOnlySpan<int> Next => _next.AsSpan(0, _activeCount);

    /// <summary>Entity mapped by span index.</summary>
    public ReadOnlySpan<Entity> EntitiesBySpanIndex => _entityBySpanIndex.AsSpan(0, _activeCount);

    public UniformGrid(float originX, float originY, float cellSize, int cellsX, int cellsY, int maxEntities = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cellSize, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cellsX, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cellsY, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntities);

        _originX = originX;
        _originY = originY;
        _cellSize = cellSize;
        _invCellSize = 1f / cellSize;
        _cellsX = cellsX;
        _cellsY = cellsY;
        _numCells = cellsX * cellsY;

        _head = new int[_numCells];
        var cap = Math.Max(maxEntities, 1);
        _next = new int[cap];
        _entityBySpanIndex = new Entity[cap];
        _positionBySpanIndex = new Vector2[cap];

        ClearHeadsParallel();
    }

    /// <summary>Ensures span-indexed buffers hold at least <paramref name="minCapacity"/> entries.</summary>
    public void EnsureSpanCapacity(int minCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minCapacity);
        if (_next.Length >= minCapacity)
            return;

        var newLen = _next.Length;
        while (newLen < minCapacity)
            newLen = newLen < 1024 ? newLen * 2 : newLen + 1024;

        Array.Resize(ref _next, newLen);
        Array.Resize(ref _entityBySpanIndex, newLen);
        Array.Resize(ref _positionBySpanIndex, newLen);
    }

    /// <summary>Parallel clear job body for grid heads.</summary>
    public void ClearHeadsParallel()
    {
        Parallel.For(0, _numCells, i => _head[i] = Empty);
    }

    /// <summary>
    /// Populates Head/Next from aligned spans. Uses atomic head exchange so multiple threads can insert
    /// into the same cell safely.
    /// </summary>
    public void PopulateParallel(ReadOnlySpan<Entity> entities, ReadOnlySpan<Vector2> positions)
    {
        if (entities.Length != positions.Length)
            throw new ArgumentException("Entities and positions spans must have identical lengths.");

        var count = entities.Length;
        if (count == 0)
        {
            _activeCount = 0;
            return;
        }

        EnsureSpanCapacity(count);
        _activeCount = count;

        for (var i = 0; i < count; i++)
        {
            _entityBySpanIndex[i] = entities[i];
            _positionBySpanIndex[i] = positions[i];
        }

        Parallel.For(0, count, i =>
        {
            var entity = _entityBySpanIndex[i];
            var pos = _positionBySpanIndex[i];
            var cell = GetCellIndex(pos);

            var prev = Interlocked.Exchange(ref _head[cell], i);
            _next[i] = prev;
        });
    }

    /// <inheritdoc />
    public void Update(ReadOnlySpan<Entity> entities, ReadOnlySpan<Vector2> positions)
    {
        ClearHeadsParallel();
        PopulateParallel(entities, positions);
    }

    /// <summary>
    /// Maps world-space position to clamped linear cell index (always in [0, CellsX*CellsY-1]).
    /// </summary>
    public int GetCellIndex(Vector2 position) => CellIndexYClamped(position.Y) * _cellsX + CellIndexXClamped(position.X);

    /// <inheritdoc />
    public void Query(Vector2 center, float radius, List<Entity> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var r = radius < 0f ? 0f : radius;
        var minX = center.X - r;
        var maxX = center.X + r;
        var minY = center.Y - r;
        var maxY = center.Y + r;

        var cx0 = CellIndexXClamped(minX);
        var cx1 = CellIndexXClamped(maxX);
        var cy0 = CellIndexYClamped(minY);
        var cy1 = CellIndexYClamped(maxY);

        for (var cy = cy0; cy <= cy1; cy++)
        {
            var row = cy * _cellsX;
            for (var cx = cx0; cx <= cx1; cx++)
            {
                var headIdx = _head[row + cx];
                while (headIdx != Empty)
                {
                    results.Add(_entityBySpanIndex[headIdx]);
                    headIdx = _next[headIdx];
                }
            }
        }
    }

    int CellIndexXClamped(float worldX)
    {
        var cx = (int)MathF.Floor((worldX - _originX) * _invCellSize);
        return Math.Clamp(cx, 0, _cellsX - 1);
    }

    int CellIndexYClamped(float worldY)
    {
        var cy = (int)MathF.Floor((worldY - _originY) * _invCellSize);
        return Math.Clamp(cy, 0, _cellsY - 1);
    }
}
