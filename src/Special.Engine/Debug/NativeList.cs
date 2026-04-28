using System.Threading;

namespace Special.Engine.Debug;

/// <summary>
/// Lightweight native-list-style container with a thread-safe parallel writer API for debug data.
/// This mirrors the shape needed by debug jobs in this engine runtime.
/// </summary>
public sealed class NativeList<T> : IDisposable where T : struct
{
    T[] _items;
    int _length;
    bool _disposed;
    readonly object _resizeLock = new();

    public NativeList(int initialCapacity = 128)
    {
        if (initialCapacity < 1)
            initialCapacity = 1;

        _items = new T[initialCapacity];
    }

    public int Length => Volatile.Read(ref _length);
    public int Capacity => _items.Length;

    public void Add(in T value)
    {
        ThrowIfDisposed();
        EnsureCapacity(Length + 1);
        var idx = _length;
        _items[idx] = value;
        _length = idx + 1;
    }

    public void EnsureCapacity(int minCapacity)
    {
        ThrowIfDisposed();
        if (minCapacity <= _items.Length)
            return;

        lock (_resizeLock)
        {
            if (minCapacity <= _items.Length)
                return;

            var newCapacity = _items.Length;
            while (newCapacity < minCapacity)
                newCapacity *= 2;

            Array.Resize(ref _items, newCapacity);
        }
    }

    public void Clear()
    {
        ThrowIfDisposed();
        _length = 0;
    }

    public ReadOnlySpan<T> AsArray()
    {
        ThrowIfDisposed();
        return _items.AsSpan(0, Length);
    }

    public ParallelWriter AsParallelWriter()
    {
        ThrowIfDisposed();
        return new ParallelWriter(this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _items = Array.Empty<T>();
        _length = 0;
    }

    void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NativeList<T>));
    }

    void AddNoResizeThreadSafe(in T value)
    {
        var items = _items;
        while (true)
        {
            var index = Volatile.Read(ref _length);
            if (index >= items.Length)
                throw new InvalidOperationException("NativeList capacity exceeded in AddNoResize.");

            if (Interlocked.CompareExchange(ref _length, index + 1, index) == index)
            {
                items[index] = value;
                return;
            }
        }
    }

    public readonly struct ParallelWriter
    {
        readonly NativeList<T> _owner;

        internal ParallelWriter(NativeList<T> owner)
        {
            _owner = owner;
        }

        public void AddNoResize(in T value)
        {
            _owner.ThrowIfDisposed();
            _owner.AddNoResizeThreadSafe(value);
        }
    }
}
