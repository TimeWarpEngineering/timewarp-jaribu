namespace TimeWarp.Jaribu;

using System.Runtime.ExceptionServices;

#region Purpose
// Ends a session and disposes every fixture that session created.
#endregion

#region Design
// Dispose starts for every created instance before the wait. A cancelled token
// still starts each DisposeAsync, observes tasks that have not finished, and
// then throws, so a later fixture is not left running unobserved.
// A DisposeAsync that throws before it returns a task is wrapped in the same
// DisposeAsync-failed exception and does not skip the remaining fixtures.
#endregion

public static partial class SessionFixture
{
  /// <summary>
  /// Ends a test session. When nesting reaches zero, disposes all created fixtures
  /// and clears instance references (registrations are kept).
  /// A cancelled token starts every dispose, observes each task, and then throws.
  /// </summary>
  /// <param name="cancellationToken">
  /// Stops waiting for fixture disposal.
  /// </param>
  /// <exception cref="OperationCanceledException">
  /// Thrown when <paramref name="cancellationToken"/> is already cancelled, or fires
  /// while dispose is still in flight, after every created fixture has been started.
  /// </exception>
  internal static async Task EndSessionAsync(CancellationToken cancellationToken = default)
  {
    List<(Type Type, IAsyncDisposable Instance)> toDispose = [];

    lock (RegistryLock)
    {
      if (Nesting <= 0)
      {
        return;
      }

      Nesting--;
      if (Nesting != 0)
      {
        return;
      }

      Generation++;

      foreach (KeyValuePair<Type, FixtureEntry> pair in Registry)
      {
        if (pair.Value.Instance is IAsyncDisposable disposable)
        {
          toDispose.Add((pair.Key, disposable));
          pair.Value.Instance = null;
        }

        // Sticky create failures are session-scoped; clear when session fully ends.
        pair.Value.CreateFailure = null;
      }
    }

    List<Exception> errors = [];
    List<(Type Type, Task DisposeTask)> pending = new(toDispose.Count);
    foreach ((Type type, IAsyncDisposable instance) in toDispose)
    {
      try
      {
        pending.Add((type, instance.DisposeAsync().AsTask()));
      }
      catch (Exception exception)
      {
        // DisposeAsync threw before it returned a task. The loop starts every remaining fixture.
        errors.Add(WrapDisposeFailure(type, exception));
      }
    }

    if (cancellationToken.IsCancellationRequested &&
        pending.Exists(static item => !item.DisposeTask.IsCompleted))
    {
      ObserveSessionDisposes(pending);
      throw new OperationCanceledException(cancellationToken);
    }

    Exception? waitFailure = null;
    try
    {
      if (pending.Count > 0)
      {
        Task[] disposeTasks = new Task[pending.Count];
        for (int index = 0; index < pending.Count; index++)
        {
          disposeTasks[index] = pending[index].DisposeTask;
        }

        await Task.WhenAll(disposeTasks).WaitAsync(cancellationToken).ConfigureAwait(false);
      }
    }
    catch (Exception exception) when (cancellationToken.IsCancellationRequested)
    {
      ObserveSessionDisposes(pending);
      throw new OperationCanceledException(
        "Session fixture dispose was cancelled.",
        exception,
        cancellationToken);
    }
    catch (Exception exception)
    {
      waitFailure = exception;
    }

    foreach ((Type type, Task disposeTask) in pending)
    {
      if (disposeTask.Status != TaskStatus.Faulted)
      {
        continue;
      }

      AggregateException aggregate = disposeTask.Exception!;
      Exception thrown = aggregate.InnerExceptions.Count == 1
        ? aggregate.InnerExceptions[0]
        : aggregate;
      errors.Add(WrapDisposeFailure(type, thrown));
    }

    if (errors.Count == 0 && waitFailure is not null)
    {
      errors.Add(new InvalidOperationException(
        "Session fixture dispose failed.",
        waitFailure));
    }

    if (errors.Count == 1)
    {
      ExceptionDispatchInfo.Capture(errors[0]).Throw();
    }
    else if (errors.Count > 1)
    {
      throw new AggregateException(
        "One or more session fixtures failed to dispose.",
        errors);
    }
  }

  private static InvalidOperationException WrapDisposeFailure(Type type, Exception exception)
  {
    return new InvalidOperationException(
      $"Session fixture '{type.FullName}' DisposeAsync failed: {exception.Message}",
      exception);
  }

  private static void ObserveSessionDisposes(List<(Type Type, Task DisposeTask)> pending)
  {
    foreach ((Type _, Task disposeTask) in pending)
    {
      TestRunner.ObserveAbandoned(disposeTask);
    }
  }
}
