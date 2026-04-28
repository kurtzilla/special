using Special.Engine.Debug;

namespace Special.Engine.Ecs.Components;

/// <summary>
/// Singleton-style debug visualization toggles.
/// Create one entity with this component to control global debug categories.
/// </summary>
public readonly struct DebugSettings
{
    public readonly DebugVisualCategory EnabledCategories;

    public DebugSettings(DebugVisualCategory enabledCategories)
    {
        EnabledCategories = enabledCategories;
    }

    public bool IsEnabled(DebugVisualCategory category) => (EnabledCategories & category) != 0;
}
