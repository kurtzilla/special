namespace Special.Engine.Ecs.Jobs;

/// <summary>Shared empty component lists for jobs that touch no pools.</summary>
public static class JobAccess
{
    public static readonly IReadOnlyList<Type> EmptyRead = Array.Empty<Type>();
    public static readonly IReadOnlyList<Type> EmptyWrite = Array.Empty<Type>();
}
