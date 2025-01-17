using Nito.Disposables;
using Nito.StructuredConcurrency;
using Nito.StructuredConcurrency.Internals;

namespace UnitTests;

public class TaskScopeUnitTests
{
    [Fact]
    public async Task WaitsForAllChildrenToComplete()
    {
        var task1Signal = new TaskCompletionSource();
        var task2Signal = new TaskCompletionSource();
        var readySignal = new TaskCompletionSource();

        Task? task1 = null;
        Task? task2 = null;

        var groupTask = TaskGoup.RunScopeAsync(default, group =>
        {
            task1 = group.RunAsync(async _ => { await task1Signal.Task; return 0; });
            task2 = group.RunAsync(async _ => { await task2Signal.Task; return 0; });
            readySignal.TrySetResult();
        });

        await readySignal.Task;

        await Assert.ThrowsAnyAsync<TimeoutException>(() => task1!.WaitAsync(TimeSpan.FromMilliseconds(100)));
        await Assert.ThrowsAnyAsync<TimeoutException>(() => task2!.WaitAsync(TimeSpan.FromMilliseconds(100)));
        await Assert.ThrowsAnyAsync<TimeoutException>(() => groupTask.WaitAsync(TimeSpan.FromMilliseconds(100)));

        task1Signal.TrySetResult();

        await task1!;
        await Assert.ThrowsAnyAsync<TimeoutException>(() => task2!.WaitAsync(TimeSpan.FromMilliseconds(100)));
        await Assert.ThrowsAnyAsync<TimeoutException>(() => groupTask.WaitAsync(TimeSpan.FromMilliseconds(100)));

        task2Signal.TrySetResult();

        await task1;
        await task2!;
        await groupTask;
    }

    [Fact]
    public async Task FaultedTask_CancelsOtherTasks()
    {
        var task1Signal = new TaskCompletionSource();
        var readySignal = new TaskCompletionSource();

        Task? task1 = null;
        Task? task2 = null;

        var groupTask = TaskGoup.RunScopeAsync(default, group =>
        {
#pragma warning disable CS0162 // Unreachable code detected
            task1 = group.RunAsync(async _ => { await task1Signal.Task; throw new InvalidOperationException("1"); return 0; });
#pragma warning restore CS0162 // Unreachable code detected
            task2 = group.RunAsync(async ct => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return 0; });
            readySignal.TrySetResult();
        });

        await readySignal.Task;
        task1Signal.TrySetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => task1!);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task2!);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => groupTask);
    }

    [Fact]
    public async Task ExternalCancellation_Ignored()
    {
        var task1Signal = new TaskCompletionSource();
        var readySignal = new TaskCompletionSource();
        var cts = new CancellationTokenSource();

        Task? task1 = null;
        Task? task2 = null;

        var groupTask = TaskGoup.RunScopeAsync(default, group =>
        {
            task1 = group.RunAsync(async _ => { await task1Signal.Task; return 0; });
            task2 = group.RunAsync(async _ => { await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token); return 0; });
            readySignal.TrySetResult();
        });

        await readySignal.Task;
        task1Signal.TrySetResult();

        await task1!;
        await Assert.ThrowsAnyAsync<TimeoutException>(() => task2!.WaitAsync(TimeSpan.FromMilliseconds(100)));
        await Assert.ThrowsAnyAsync<TimeoutException>(() => groupTask.WaitAsync(TimeSpan.FromMilliseconds(100)));

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task2!);
        await groupTask;
    }

    [Fact]
    public async Task EmptyGroup_NoDeadlock()
    {
        await TaskGoup.RunScopeAsync(default, group => { });
    }

    [Fact]
    public async Task Resource_DisposedAtEndOfTaskGroup()
    {
        int wasdisposed = 0;

        await TaskGoup.RunScopeAsync(default, async group =>
        {
            await group.AddResourceAsync(Disposable.Create(() => Interlocked.Exchange(ref wasdisposed, 1)));
        });
        var result = Interlocked.CompareExchange(ref wasdisposed, 0, 0);
        Assert.Equal(1, wasdisposed);
    }

    [Fact]
    public async Task Resource_ThrowsException_Ignored()
    {
        await TaskGoup.RunScopeAsync(default, async group =>
        {
            await group.AddResourceAsync(Disposable.Create(() => throw new InvalidOperationException("nope")));
        });
    }

    [Fact]
    public async Task ReturnValue_NotAResource()
    {
        int wasdisposed = 0;

        await TaskGoup.RunScopeAsync(default, async group =>
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            var resource = await group.RunAsync(async ct => Disposable.Create(() => Interlocked.Exchange(ref wasdisposed, 1)));
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        });
        var result = Interlocked.CompareExchange(ref wasdisposed, 0, 0);
        Assert.Equal(0, wasdisposed);
    }
}