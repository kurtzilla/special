namespace Special.Engine.Ecs.Query;

/// <summary>
/// Aligned read-only views over query staging buffers (same length; index pairs match <see cref="Query{T1,T2}.Entities"/>).
/// </summary>
public readonly ref struct AlignedQuerySpans<T1, T2>
    where T1 : struct
    where T2 : struct
{
    /// <summary>First component values, aligned with <see cref="Values2"/> by index.</summary>
    public ReadOnlySpan<T1> Values1 { get; }

    /// <summary>Second component values, aligned with <see cref="Values1"/> by index.</summary>
    public ReadOnlySpan<T2> Values2 { get; }

    internal AlignedQuerySpans(ReadOnlySpan<T1> values1, ReadOnlySpan<T2> values2)
    {
        Values1 = values1;
        Values2 = values2;
    }
}
