using Nito.StructuredConcurrency.Advanced;
using Nito.StructuredConcurrency.Internals;

namespace Nito.StructuredConcurrency;

#pragma warning disable CA1068 // CancellationToken parameters must come last

/// <summary>
/// Provides methods for creating and running different types of task groups.
/// </summary>
public static class TaskScope
{
    /// <summary>
    /// Creates a new <see cref="RunTaskScope"/> and runs the specified work as the first work task.
    /// </summary>
    /// <typeparam name="T">The type of the result of the task.</typeparam>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static async Task<T> RunScopeAsync<T>(CancellationToken cancellationToken, Func<RunTaskScope, ValueTask<T>> work)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var group = new RunTaskScope(new TaskScopeCore(cancellationToken));
#pragma warning restore CA2000 // Dispose objects before losing scope
        await using (group.ConfigureAwait(false))
            return await group.DoRunAsync(_ => work(group)).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new <see cref="RunTaskScope"/> and runs the specified work as the first work task.
    /// </summary>
    /// <typeparam name="T">The type of the result of the task.</typeparam>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task<T> RunScopeAsync<T>(CancellationToken cancellationToken, Func<RunTaskScope, T> work) =>
        RunScopeAsync(cancellationToken, work.AsAsync());

    /// <summary>
    /// Creates a new <see cref="RunTaskScope"/> and runs the specified work as the first work task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task RunScopeAsync(CancellationToken cancellationToken, Func<RunTaskScope, ValueTask> work) =>
        RunScopeAsync(cancellationToken, work.WithResult());

    /// <summary>
    /// Creates a new <see cref="RunTaskScope"/> and runs the specified work as the first work task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task RunScopeAsync(CancellationToken cancellationToken, Action<RunTaskScope> work) =>
        RunScopeAsync(cancellationToken, work.AsAsync().WithResult());

    /// <summary>
    /// Creates a new <see cref="RaceTaskScope{TResult}"/> and runs the specified work as the first run task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first run task of the task group.</param>
    public static Task<T> RaceScopeAsync<T>(CancellationToken cancellationToken, Func<RaceTaskScope<T>, ValueTask> work) =>
        RaceTaskScope<T>.RaceScopeAsync(cancellationToken, work);

    /// <summary>
    /// Creates a new <see cref="RaceTaskScope{TResult}"/> and runs the specified work as the first run task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first run task of the task group.</param>
    public static Task<T> RaceScopeAsync<T>(CancellationToken cancellationToken, Action<RaceTaskScope<T>> work) =>
        RaceTaskScope<T>.RaceScopeAsync(cancellationToken, work.AsAsync());
}
