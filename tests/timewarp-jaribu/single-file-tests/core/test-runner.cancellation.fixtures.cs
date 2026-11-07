#region Purpose
// Scenario fixtures for cancellation. These classes are not registered.
#endregion

#region Design
// Each fixture blocks on a TaskCompletionSource the meta-test releases.
// A delay would hide a missed cancel behind the runner timeout.
// SyncThrowCleanUpOnceFixture does not block. It throws before returning a task.
#endregion

namespace TestRunner_;

public class HoldPairFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static int Entered;
  public static int Finished;

  public static async Task First_Should_Block()
  {
    Entered++;
    Cancellation.Cancel();
    await Release.Task;
    Finished++;
  }

  public static async Task Second_Should_NotStart()
  {
    Entered++;
    Cancellation.Cancel();
    await Release.Task;
    Finished++;
  }
}

public class LaterClassFixture
{
  public static int Entered;

  public static async Task Should_NotStart()
  {
    Entered++;
    await Task.CompletedTask;
  }
}

public class QuickFixture
{
  public static int Entered;

  public static async Task Should_NotStart()
  {
    Entered++;
    await Task.CompletedTask;
  }
}

public class TimeoutHoldFixture
{
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

  [Timeout(150)]
  public static async Task Hold_Should_Timeout()
  {
    await Release.Task;
  }
}

public class CancelBeatsTimeoutFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static int Entered;

  [Timeout(10000)]
  public static async Task Hold_Should_BeCancelled()
  {
    Entered++;
    Cancellation.Cancel();
    await Release.Task;
  }
}

public class SetupHoldFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static int Body;
  public static int SetupFinished;
  public static int CleanUpEntered;

  public static void Bind(CancellationTokenSource cancellation, TaskCompletionSource release)
  {
    Cancellation = cancellation;
    Release = release;
    Body = 0;
    SetupFinished = 0;
    CleanUpEntered = 0;
  }

  public static async Task Setup()
  {
    Cancellation.Cancel();
    await Release.Task;
    SetupFinished++;
  }

  public static async Task CleanUp()
  {
    CleanUpEntered++;
    await Task.CompletedTask;
  }

  public static async Task Body_Should_NotRun()
  {
    Body++;
    await Task.CompletedTask;
  }
}

public class SetupOnceHoldFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static int Body;
  public static int CleanUpOnceEntered;

  public static void Bind(CancellationTokenSource cancellation, TaskCompletionSource release)
  {
    Cancellation = cancellation;
    Release = release;
    Body = 0;
    CleanUpOnceEntered = 0;
  }

  public static async Task SetupOnce()
  {
    Cancellation.Cancel();
    await Release.Task;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceEntered++;
    await Task.CompletedTask;
  }

  public static async Task Body_Should_NotRun()
  {
    Body++;
    await Task.CompletedTask;
  }
}

public class SyncThrowCleanUpOnceFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static int Body;
  public static int CleanUpOnceEntered;

  public static void Bind(CancellationTokenSource cancellation)
  {
    Cancellation = cancellation;
    Body = 0;
    CleanUpOnceEntered = 0;
  }

  public static Task SetupOnce() => Task.CompletedTask;

  public static Task CleanUpOnce()
  {
    CleanUpOnceEntered++;
    throw new InvalidOperationException("CleanUpOnce sync");
  }

  public static async Task Body_Should_Cancel()
  {
    Body++;
    Cancellation.Cancel();
    await Task.CompletedTask;
  }
}

public class CleanUpHoldFixture
{
  public static CancellationTokenSource Cancellation = new();
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static int Body;
  public static int CleanUpFinished;

  public static void Bind(CancellationTokenSource cancellation, TaskCompletionSource release)
  {
    Cancellation = cancellation;
    Release = release;
    Body = 0;
    CleanUpFinished = 0;
  }

  public static async Task Body_Should_Run()
  {
    Body++;
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    Cancellation.Cancel();
    await Release.Task;
    CleanUpFinished++;
  }
}

public sealed class SlowCreateFixture : IAsyncDisposable
{
  public static int Entered;
  public static int Finished;
  public static TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static void Reset(TaskCompletionSource started, TaskCompletionSource release)
  {
    Entered = 0;
    Finished = 0;
    Started = started;
    Release = release;
  }

  public static async Task<SlowCreateFixture> CreateAsync()
  {
    Entered++;
    Started.TrySetResult();
    await Release.Task;
    Finished++;
    return new SlowCreateFixture();
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class SlowDisposeFixture : IAsyncDisposable
{
  public static int Entered;
  public static int Finished;
  public static TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static void Reset(TaskCompletionSource started, TaskCompletionSource release)
  {
    Entered = 0;
    Finished = 0;
    Started = started;
    Release = release;
  }

  public static Task<SlowDisposeFixture> CreateAsync() => Task.FromResult(new SlowDisposeFixture());

  public async ValueTask DisposeAsync()
  {
    Entered++;
    Started.TrySetResult();
    await Release.Task;
    Finished++;
  }
}

public sealed class FirstCancelledDisposeFixture : IAsyncDisposable
{
  public static int Entered;
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static void Reset(TaskCompletionSource release, TaskCompletionSource finished)
  {
    Entered = 0;
    Release = release;
    Finished = finished;
  }

  public static Task<FirstCancelledDisposeFixture> CreateAsync() =>
    Task.FromResult(new FirstCancelledDisposeFixture());

  public async ValueTask DisposeAsync()
  {
    Entered++;
    await Release.Task;
    Finished.TrySetResult();
  }
}

public sealed class SecondCancelledDisposeFixture : IAsyncDisposable
{
  public static int Entered;
  public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
  public static TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static void Reset(TaskCompletionSource release, TaskCompletionSource finished)
  {
    Entered = 0;
    Release = release;
    Finished = finished;
  }

  public static Task<SecondCancelledDisposeFixture> CreateAsync() =>
    Task.FromResult(new SecondCancelledDisposeFixture());

  public async ValueTask DisposeAsync()
  {
    Entered++;
    await Release.Task;
    Finished.TrySetResult();
  }
}
