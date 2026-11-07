#region Purpose
// Fixtures and the manual clock for timeout-policy tests. These classes are not registered.
#endregion

#region Design
// The recording provider stores timers instead of waiting on the system clock.
// The meta-test fires them. A class or DefaultHookTimeout is what creates a hook timer.
#endregion

namespace TestRunner_;

using System.Diagnostics;

sealed class TimeoutPolicyCollectingSink : ITestResultSink
{
  public List<TestNodeInfo> Results { get; } = [];

  public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
  public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    Results.Add(node);
    return Task.CompletedTask;
  }

  public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;
  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
}

sealed class ClockScope : IDisposable
{
  private readonly TimeProvider PreviousProvider;
  private readonly TimeSpan? PreviousTimeout;

  public ClockScope(TimeSpan? hookTimeout = null)
  {
    PreviousProvider = TestRunner.TimeProvider;
    PreviousTimeout = TestRunner.DefaultHookTimeout;
    Provider = new RecordingTimeProvider();
    TestRunner.TimeProvider = Provider;
    TestRunner.DefaultHookTimeout = hookTimeout;
  }

  public RecordingTimeProvider Provider { get; }

  public void Dispose()
  {
    TestRunner.TimeProvider = PreviousProvider;
    TestRunner.DefaultHookTimeout = PreviousTimeout;
  }
}

sealed class RecordingTimeProvider : TimeProvider
{
  private readonly Lock Gate = new();
  private readonly List<RecordingTimer> StoredTimers = [];

  public int TimerCount
  {
    get
    {
      lock (Gate)
      {
        return StoredTimers.Count;
      }
    }
  }

  public RecordingTimer TimerAt(int index)
  {
    lock (Gate)
    {
      return StoredTimers[index];
    }
  }

  public bool HasLiveTimer()
  {
    lock (Gate)
    {
      return StoredTimers.Exists(static timer => !timer.Disposed);
    }
  }

  public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
  {
    RecordingTimer timer = new(callback, state);
    lock (Gate)
    {
      StoredTimers.Add(timer);
    }

    return timer;
  }

  public void FireAll()
  {
    RecordingTimer[] snapshot;
    lock (Gate)
    {
      snapshot = [.. StoredTimers];
    }

    foreach (RecordingTimer timer in snapshot)
    {
      timer.Fire();
    }
  }
}

sealed class RecordingTimer : ITimer
{
  private readonly Lock Gate = new();
  private readonly TimerCallback Callback;
  private readonly object? State;
  private bool IsDisposed;

  public RecordingTimer(TimerCallback callback, object? state)
  {
    Callback = callback;
    State = state;
  }

  /// <summary>
  /// Runs before the timer callback, so a fault stored here is visible when the timeout is signaled.
  /// </summary>
  public Action? BeforeFire { get; set; }

  public bool Disposed
  {
    get
    {
      lock (Gate)
      {
        return IsDisposed;
      }
    }
  }

  public void Fire()
  {
    lock (Gate)
    {
      if (IsDisposed)
      {
        return;
      }
    }

    BeforeFire?.Invoke();
    try
    {
      Callback(State);
    }
    catch (ObjectDisposedException)
    {
      // The wait already observed the stored fault and disposed the source.
    }
  }

  public bool Change(TimeSpan dueTime, TimeSpan period) => !Disposed;

  public void Dispose()
  {
    lock (Gate)
    {
      IsDisposed = true;
    }
  }

  public ValueTask DisposeAsync()
  {
    Dispose();
    return ValueTask.CompletedTask;
  }
}

sealed class LineListener : TraceListener
{
  private readonly Lock Gate = new();
  private string Buffer = "";

  public string Text
  {
    get
    {
      lock (Gate)
      {
        return Buffer;
      }
    }
  }

  public override void Write(string? message)
  {
    lock (Gate)
    {
      Buffer += message;
    }
  }

  public override void WriteLine(string? message)
  {
    lock (Gate)
    {
      Buffer += message;
    }
  }
}

public class FastTimeoutFixture
{
  [Timeout(60_000)]
  public static async Task Only() => await Task.CompletedTask;
}

public class NoBudgetFixture
{
  public static async Task Setup() => await Task.CompletedTask;

  public static async Task Only() => await Task.CompletedTask;
}

public class ThrowsTimeoutFixture
{
  public static async Task Only()
  {
    throw new TimeoutException("body timeout");
  }
}

public class LateFaultFixture
{
  public static TaskCompletionSource Gate = Gates.New();

  public static void Reset() => Gate = Gates.New();

  [Timeout(60_000)]
  public static async Task Only()
  {
    await Gate.Task;
    throw new InvalidOperationException("late boom");
  }
}

[Timeout(60_000)]
public class SetupHangFixture
{
  public static TaskCompletionSource Gate = Gates.New();
  public static int Body;
  public static int CleanUpCount;

  public static void Reset()
  {
    Gate = Gates.New();
    Body = 0;
    CleanUpCount = 0;
  }

  public static async Task Setup() => await Gate.Task;

  public static async Task CleanUp()
  {
    CleanUpCount++;
    await Task.CompletedTask;
  }

  public static async Task Only()
  {
    Body++;
    await Task.CompletedTask;
  }
}

public class CleanUpHangFixture
{
  public static TaskCompletionSource Gate = Gates.New();
  public static int Body;

  public static void Reset()
  {
    Gate = Gates.New();
    Body = 0;
  }

  public static async Task CleanUp() => await Gate.Task;

  public static async Task Only()
  {
    Body++;
    await Task.CompletedTask;
  }
}

[Timeout(60_000)]
public class SetupOnceHangFixture
{
  public static TaskCompletionSource Gate = Gates.New();
  public static int Body;

  public static void Reset()
  {
    Gate = Gates.New();
    Body = 0;
  }

  public static async Task SetupOnce() => await Gate.Task;

  public static async Task One()
  {
    Body++;
    await Task.CompletedTask;
  }

  public static async Task Two()
  {
    Body++;
    await Task.CompletedTask;
  }
}

[Timeout(60_000)]
public class CleanUpOnceHangFixture
{
  public static TaskCompletionSource Gate = Gates.New();

  public static int Entered;

  public static void Reset()
  {
    Gate = Gates.New();
    Entered = 0;
  }

  public static async Task SetupOnce() => await Task.CompletedTask;

  public static async Task CleanUpOnce()
  {
    Entered++;
    await Gate.Task;
  }

  public static async Task Only() => await Task.CompletedTask;
}

public sealed class CreateHangFixture : IAsyncDisposable
{
  public static TaskCompletionSource Gate = Gates.New();
  public static TaskCompletionSource Disposed = Gates.New();
  public static int Started;

  public static void Reset()
  {
    Gate = Gates.New();
    Disposed = Gates.New();
    Started = 0;
  }

  public static async Task<CreateHangFixture> CreateAsync()
  {
    Started++;
    await Gate.Task;
    return new CreateHangFixture();
  }

  public ValueTask DisposeAsync()
  {
    Disposed.TrySetResult();
    return ValueTask.CompletedTask;
  }
}

[Timeout(60_000)]
public class UsesCreateHangFixture
{
  public static async Task Only()
  {
    _ = await SessionFixture.GetAsync<CreateHangFixture>();
  }
}

public class FaultAtFireFixture
{
  public static TaskCompletionSource Gate = new();

  public static void Reset() => Gate = new();

  [Timeout(60_000)]
  public static async Task Only() => await Gate.Task;
}

public sealed class RetryAfterTimeoutFixture : IAsyncDisposable
{
  public static TaskCompletionSource Gate = Gates.New();
  public static int Started;

  public static void Reset()
  {
    Gate = Gates.New();
    Started = 0;
  }

  public static async Task<RetryAfterTimeoutFixture> CreateAsync()
  {
    int attempt = Interlocked.Increment(ref Started);
    if (attempt == 1)
    {
      await Gate.Task;
    }

    return new RetryAfterTimeoutFixture();
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

[Timeout(60_000)]
public class TimesOutCreateFixture
{
  public static async Task First()
  {
    _ = await SessionFixture.GetAsync<RetryAfterTimeoutFixture>();
  }
}

public class RetriesCreateFixture
{
  public static async Task Second()
  {
    RetryAfterTimeoutFixture fixture = await SessionFixture.GetAsync<RetryAfterTimeoutFixture>();
    fixture.ShouldNotBeNull();
  }
}

static class Gates
{
  public static TaskCompletionSource New() =>
    new(TaskCreationOptions.RunContinuationsAsynchronously);
}
