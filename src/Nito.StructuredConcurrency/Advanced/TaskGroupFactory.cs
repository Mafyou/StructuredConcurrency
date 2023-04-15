namespace Nito.StructuredConcurrency.Advanced;

/// <summary>
/// Provides advanced methods for creating task groups with non-standard lifetimes.
/// </summary>
public static class TaskGroupFactory
{
    /// <inheritdoc cref="TaskScopeCore.TaskScopeCore"/>
    public static TaskScopeCore CreateTaskGroupCore(CancellationToken cancellationToken) => new(cancellationToken);

    /// <inheritdoc cref="RunTaskScope.RunTaskScope"/>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public static RunTaskScope CreateRunTaskGroup(CancellationToken cancellationToken) => new(new(cancellationToken));
#pragma warning restore CA2000 // Dispose objects before losing scope
}
