namespace Special.Engine.Ecs;

/// <summary>
/// Central place to register <see cref="Registry.RegisterComponentCloner{T}"/> for struct components that contain
/// reference types (see <see cref="Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>).
/// </summary>
/// <remarks>
/// <para><b>Safe (POD) components</b> — fields are only plain scalars and blittable-style structs (e.g. <c>int</c>, <c>float</c>, <c>bool</c>,
/// <see cref="System.Numerics.Vector3"/>, <see cref="System.Numerics.Quaternion"/>, nested structs that themselves contain no references).
/// A normal value copy on clone is correct. <b>Do not register a cloner for pure POD types</b> — it adds overhead with no benefit.</para>
/// <para><b>Risk (reference-bearing) components</b> — fields such as <c>string</c>, <c>List&lt;T&gt;</c>, <c>Dictionary&lt;,&gt;</c>, or references to custom classes.
/// Register <see cref="Registry.RegisterComponentCloner{T}"/> with a function that returns a <b>new</b> struct and <b>new</b> collections so cloned entities
/// do not share mutable reference state with the source, e.g. <c>original =&gt; new MyComponent { Id = original.Id, Items = new List&lt;int&gt;(original.Items) }</c>.</para>
/// <para><b>Many cloners = design smell.</b> If most components need custom cloners because they embed lists or dictionaries, prefer refactoring:
/// move data to a dense buffer / SoA, a globally managed pool, or store <see cref="Entity"/> or index handles into that storage instead of owning
/// <c>List&lt;&gt;</c> on the component. That keeps clones cheap, iteration cache-friendly, and avoids a large cloner table. Ask whether reference fields can be
/// replaced with handles or external pools before adding more cloners.</para>
/// <para><b>Engine audit</b> (<c>Special.Engine.Ecs.Components</c>):</para>
/// <list type="bullet">
/// <item><see cref="Components.Position"/> — POD.</item>
/// <item><see cref="Components.Velocity"/> — POD.</item>
/// <item><see cref="Components.SpriteTag"/> — POD.</item>
/// <item><see cref="Components.Transform"/> — POD (<see cref="System.Numerics.Vector3"/> only).</item>
/// <item><see cref="Components.Collider"/> — POD.</item>
/// </list>
/// <para>None require a cloner today. When you add reference-bearing component types, register them in <see cref="RegisterReferenceComponentCloners"/>
/// (invoked from <see cref="EcsWorld"/> construction).</para>
/// </remarks>
public static class ComponentCloneRegistration
{
    /// <summary>
    /// Registers cloners for built-in engine components that contain references. Extend when new such types are added.
    /// </summary>
    public static void RegisterReferenceComponentCloners(Registry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        // Example when a reference-bearing component exists:
        // registry.RegisterComponentCloner<MyComponent>(original => new MyComponent {
        //     Id = original.Id,
        //     Items = new List<int>(original.Items),
        // });
    }
}
