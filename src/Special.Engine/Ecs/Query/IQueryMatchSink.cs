namespace Special.Engine.Ecs.Query;

/// <summary>
/// Receives structural add/remove notifications for a component type so query match sets can stay incrementally consistent.
/// </summary>
internal interface IQueryMatchSink
{
    /// <summary>Called after a pool successfully adds or removes a row for <paramref name="entity"/>.</summary>
    void OnComponentStructural(Type componentType, Entity entity, bool added);

    /// <summary>Ensures per-slot lookup arrays cover indices <c>[0, minSlots)</c>.</summary>
    void EnsureSlotMapCapacity(int minSlots);
}
