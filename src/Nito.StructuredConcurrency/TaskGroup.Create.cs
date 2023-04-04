﻿using Nito.StructuredConcurrency.Advanced;
using Nito.StructuredConcurrency.Internals;

namespace Nito.StructuredConcurrency;

#pragma warning disable CA1068 // CancellationToken parameters must come last

public sealed partial class TaskGroup
{
    /// <summary>
    /// Creates a new <see cref="TaskGroup"/> and runs the specified work as the first work task.
    /// </summary>
    /// <typeparam name="T">The type of the result of the task.</typeparam>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static async Task<T> RunGroupAsync<T>(CancellationToken cancellationToken, Func<TaskGroup, ValueTask<T>> work)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var group = new TaskGroup(new WorkTaskGroup(cancellationToken));
#pragma warning restore CA2000 // Dispose objects before losing scope
        try
        {
            return await group.RunAsync(_ => work(group)).ConfigureAwait(false);
        }
        finally
        {
            await group.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a new <see cref="TaskGroup"/> and runs the specified work as the first work task.
    /// </summary>
    /// <typeparam name="T">The type of the result of the task.</typeparam>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task<T> RunGroupAsync<T>(CancellationToken cancellationToken, Func<TaskGroup, T> work) =>
        RunGroupAsync(cancellationToken, work.AsAsync());

    /// <summary>
    /// Creates a new <see cref="TaskGroup"/> and runs the specified work as the first work task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task RunGroupAsync(CancellationToken cancellationToken, Func<TaskGroup, ValueTask> work) =>
        RunGroupAsync(cancellationToken, work.WithResult());

    /// <summary>
    /// Creates a new <see cref="TaskGroup"/> and runs the specified work as the first work task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first work task of the task group.</param>
    public static Task RunGroupAsync(CancellationToken cancellationToken, Action<TaskGroup> work) =>
        RunGroupAsync(cancellationToken, work.AsAsync().WithResult());

    /// <summary>
    /// Creates a new <see cref="RacingTaskGroup{TResult}"/> and runs the specified work as the first run task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first run task of the task group.</param>
    public static async Task<T> RaceGroupAsync<T>(CancellationToken cancellationToken, Func<RacingTaskGroup<T>, ValueTask> work)
    {
        var raceResult = new RaceResult<T>();
        
        var workGroup = new WorkTaskGroup(cancellationToken);
        var runWorkGroup = new TaskGroup(workGroup);
        var raceGroup = new RacingTaskGroup<T>(workGroup, raceResult);

        runWorkGroup.Run(_ => work(raceGroup));
        
        await raceGroup.DisposeAsync().ConfigureAwait(false);
        await runWorkGroup.DisposeAsync().ConfigureAwait(false);
        await workGroup.DisposeAsync().ConfigureAwait(false);

        return raceResult.GetResult();
    }

    /// <summary>
    /// Creates a new <see cref="RacingTaskGroup{TResult}"/> and runs the specified work as the first run task.
    /// </summary>
    /// <param name="cancellationToken">An upstream cancellation token for the task group.</param>
    /// <param name="work">The first run task of the task group.</param>
    public static Task<T> RaceGroupAsync<T>(CancellationToken cancellationToken, Action<RacingTaskGroup<T>> work) =>
        RaceGroupAsync<T>(cancellationToken, work.AsAsync());
}
