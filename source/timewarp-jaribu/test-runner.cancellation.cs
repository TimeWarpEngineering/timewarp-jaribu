namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

#region Purpose
// Stops waiting on in-flight tests, hooks, and session fixtures when a run is cancelled.
#endregion

#region Design
// Test methods and once-hooks stay parameterless. Invoke does not supply a
// CancellationToken argument, so a test that declares one still fails parameter
// count. The runner stops waiting when the caller token fires. A method that
// blocks the thread without awaiting still runs to completion.
//
// Cancelled wins over Timeout when both are signaled. A Timeout attribute with
// no cancellation still reports Timeout. The wait uses CancellationTokenSource
// and TimeProvider. Disposing that source disposes the timer when the wait ends.
// The timeout signal resumes the wait after the timer callback returns.
// A fault stored before that signal is the node outcome. A task abandoned by timeout
// is observed. A fault already stored on it is thrown so the node reports that
// fault. A fault that arrives later is traced. A success that loses to the
// timeout token is Timeout.
//
// The ambient token lets SessionFixture.GetAsync observe the active run without
// a new argument at every call site. An explicit token on GetAsync wins.
// Late create and dispose tasks are observed so cancellation does not surface
// as an unobserved exception, and a create that finishes late is disposed.
// Body and hook tasks abandoned by the wait are observed the same way, without
// disposing their result. CleanUp may overlap that still-running body.
#endregion

public static partial class TestRunner
{
  private static readonly AsyncLocal<CancellationToken> AmbientCancellation = new();

  /// <summary>
  /// Token for the run that is executing on this async flow.
  /// Session fixture creation reads it when the caller does not pass one.
  /// </summary>
  internal static CancellationToken AmbientCancellationToken => AmbientCancellation.Value;

  internal enum WaitKind
  {
    Completed,
    Cancelled,
    TimedOut
  }

  /// <summary>
  /// Restores the previous ambient token when disposed.
  /// </summary>
  private sealed class CancellationScope : IDisposable
  {
    private readonly CancellationToken Previous;
    private bool Disposed;

    public CancellationScope(CancellationToken cancellationToken)
    {
      Previous = AmbientCancellation.Value;
      AmbientCancellation.Value = cancellationToken;
    }

    public void Dispose()
    {
      if (Disposed)
      {
        return;
      }

      Disposed = true;
      AmbientCancellation.Value = Previous;
    }
  }

  /// <summary>
  /// Waits for <paramref name="task"/>. A fired caller token returns
  /// <see cref="WaitKind.Cancelled"/>. A timeout with no cancellation returns
  /// <see cref="WaitKind.TimedOut"/>. The task itself is not aborted.
  /// When <paramref name="disposeAbandonedResult"/> is true, a late successful
  /// result that implements <see cref="IAsyncDisposable"/> is disposed.
  /// </summary>
  internal static async Task<WaitKind> WaitForCompletionAsync(
    Task task,
    int? timeoutMilliseconds,
    CancellationToken cancellationToken,
    bool disposeAbandonedResult = false)
  {
    ArgumentNullException.ThrowIfNull(task);
    if (!cancellationToken.CanBeCanceled && timeoutMilliseconds is null)
    {
      await task.ConfigureAwait(false);
      return WaitKind.Completed;
    }

    if (timeoutMilliseconds is null)
    {
      try
      {
        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return cancellationToken.IsCancellationRequested
          ? MarkCancelled(task, disposeAbandonedResult)
          : WaitKind.Completed;
      }
      catch (Exception) when (cancellationToken.IsCancellationRequested)
      {
        return MarkCancelled(task, disposeAbandonedResult);
      }
    }

    using CancellationTokenSource timeoutSource = new(
      TimeSpan.FromMilliseconds(timeoutMilliseconds.Value),
      TimeProvider);
    CancellationTokenRegistration timeoutRegistration = default;
    CancellationTokenRegistration callerRegistration = default;
    try
    {
      // Resume after the timer callback returns. A fault stored in that callback
      // is then visible to MarkTimedOut, instead of racing the callback's stack.
      TaskCompletionSource<bool> signaled = new(TaskCreationOptions.RunContinuationsAsynchronously);
      timeoutRegistration = timeoutSource.Token.UnsafeRegister(
        static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
        signaled);
      if (cancellationToken.CanBeCanceled)
      {
        callerRegistration = cancellationToken.UnsafeRegister(
          static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
          signaled);
      }

      if (task.IsCompleted)
      {
        signaled.TrySetResult(true);
      }
      else
      {
        _ = task.ContinueWith(
          static (_, state) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
          signaled,
          CancellationToken.None,
          TaskContinuationOptions.ExecuteSynchronously,
          TaskScheduler.Default);
      }

      await signaled.Task.ConfigureAwait(false);
      if (cancellationToken.IsCancellationRequested)
      {
        return MarkCancelled(task, disposeAbandonedResult);
      }

      if (timeoutSource.IsCancellationRequested)
      {
        return MarkTimedOut(task, disposeAbandonedResult);
      }

      await task.ConfigureAwait(false);
      return WaitKind.Completed;
    }
    finally
    {
      await timeoutRegistration.DisposeAsync().ConfigureAwait(false);
      await callerRegistration.DisposeAsync().ConfigureAwait(false);
    }
  }

  private static int ExitCodeFor(TestRunStats stats, CancellationToken cancellationToken)
  {
    if (cancellationToken.IsCancellationRequested || !stats.Success)
    {
      return 1;
    }

    return 0;
  }

  /// <summary>
  /// Observes a task the run stopped waiting on.
  /// When <paramref name="disposeResult"/> is true, a late fixture instance is disposed.
  /// Body and hook tasks pass false so their result is left alone.
  /// </summary>
  internal static void ObserveAbandoned(Task task, bool disposeResult = false)
  {
    ArgumentNullException.ThrowIfNull(task);
    if (task.IsCompleted)
    {
      ObserveCompleted(task, disposeResult);
      return;
    }

    _ = task.ContinueWith(
      static (completed, state) => ObserveCompleted(completed, state is true),
      disposeResult,
      CancellationToken.None,
      TaskContinuationOptions.ExecuteSynchronously,
      TaskScheduler.Default);
  }

  private static WaitKind MarkCancelled(Task task, bool disposeResult = false)
  {
    ObserveAbandoned(task, disposeResult);
    return WaitKind.Cancelled;
  }

  /// <summary>
  /// A fault already stored on the task is rethrown so the caller reports it.
  /// A task that is still running is observed, and a later fault is traced.
  /// </summary>
  private static WaitKind MarkTimedOut(Task task, bool disposeResult)
  {
    if (task.IsFaulted)
    {
      Exception fault = task.Exception?.GetBaseException()
        ?? new TimeoutException("Timed out.");
      _ = task.Exception;
      ExceptionDispatchInfo.Capture(fault).Throw();
    }

    ObserveAbandoned(task, disposeResult);
    return WaitKind.TimedOut;
  }

  private static void ObserveCompleted(Task completed, bool disposeResult)
  {
    Exception? fault = completed.Exception;
    if (fault is not null)
    {
      Trace.TraceError("Abandoned task faulted: {0}", fault.GetBaseException());
    }

    if (disposeResult && completed.Status == TaskStatus.RanToCompletion)
    {
      DisposeAbandonedResult(completed);
    }
  }

  private static void DisposeAbandonedResult(Task task)
  {
    PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
    if (resultProperty?.GetValue(task) is not IAsyncDisposable disposable)
    {
      return;
    }

    try
    {
      ValueTask pending = disposable.DisposeAsync();
      if (!pending.IsCompletedSuccessfully)
      {
        _ = pending.AsTask().ContinueWith(
          static inner => _ = inner.Exception,
          CancellationToken.None,
          TaskContinuationOptions.OnlyOnFaulted,
          TaskScheduler.Default);
      }
    }
#pragma warning disable CA1031 // Late dispose is not a node. Record it so the abandoned path cannot crash the process.
    catch (Exception exception)
#pragma warning restore CA1031
    {
      Trace.TraceError("Abandoned fixture dispose failed: {0}", exception);
    }
  }
}
