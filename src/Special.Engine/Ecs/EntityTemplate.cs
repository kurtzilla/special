namespace Special.Engine.Ecs;

/// <summary>
/// Blueprint of component adds applied in order when instantiated via <see cref="Registry.Instantiate"/>.
/// Build once (e.g. at load); each <see cref="Instantiate"/> creates one entity and runs stored apply actions.
/// </summary>
public sealed class EntityTemplate
{
    readonly List<Action<Registry, Entity>> _steps = new();

    /// <summary>Adds a component value when the template is applied (fluent).</summary>
    public EntityTemplate Add<T>(T value)
        where T : struct
    {
        _steps.Add((registry, entity) => _ = registry.GetPool<T>().TryAdd(entity, value));
        return this;
    }

    /// <summary>Adds a component using a factory invoked at apply time (fluent).</summary>
    public EntityTemplate Add<T>(Func<T> factory)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(factory);
        _steps.Add((registry, entity) => _ = registry.GetPool<T>().TryAdd(entity, factory()));
        return this;
    }

    /// <summary>Number of queued add steps.</summary>
    public int StepCount => _steps.Count;

    /// <summary>Runs all queued steps on <paramref name="entity"/>.</summary>
    public void Apply(Registry registry, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(registry);
        for (var i = 0; i < _steps.Count; i++)
            _steps[i](registry, entity);
    }
}
