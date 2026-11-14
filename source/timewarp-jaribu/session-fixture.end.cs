namespace TimeWarp.Jaribu;

using System.Runtime.ExceptionServices;

#region Purpose
// Ends a session and disposes every fixture that session created.
#endregion

#region Design
// DisposeAsync starts in reverse creation order, newest first. The start loop
// does not wait, so a cancelled token still enters every DisposeAsync before
// the end throws. Tasks that have not finished are observed.
// A DisposeAsync that throws before it returns a task is wrapped in the same
// DisposeAsync-failed exception and does not skip the remaining fixtures.
// Host end and run-lease end share that dispose path. Either one disposes only
// when both depths are zero. A clear-epoch mismatch means Clear() already
// abandoned that begin, so the end returns.
#endregion

public static partial class SessionFixture
{
  /// <summary>
  /// Ends a host session. When host depth and run depth are both zero, disposes
  /// created fixtures in reverse creation order and clears instance references
  /// (registrations are kept).
  /// A cancelled token starts every dispose, observes each task, and then throws.
  /// </summary>
  /// <param name="cancellationToken">
  /// Stops waiting for fixture disposal.
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no host session is active.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  /// Thrown when <paramref name="cancellationToken"/> is already cancelled, or fires
  /// while dispose is still in flight, after every created fixture has been started.
  /// </exception>
  internal static Task EndSessionAsync(CancellationToken cancellationToken = default)
  {
    return EndHostAsync(clearEpoch: null, cancellationToken);
  }

  /// <summary>
  /// Ends the host session that captured <paramref name="clearEpoch"/>.
  /// When <see cref="Clear"/> has run since that begin, returns without throwing
  /// and without disposing.
  /// </summary>
  /// <param name="clearEpoch">Epoch returned by <see cref="BeginSession"/>.</param>
  /// <param name="cancellationToken">Stops waiting for fixture disposal.</param>
  internal static Task EndHostSessionAsync(
    long clearEpoch,
    CancellationToken cancellationToken = default)
  {
    return EndHostAsync(clearEpoch, cancellationToken);
  }

  /// <summary>
  /// Ends one run lease. Disposes when this lease was the last open depth.
  /// A stale <paramref name="clearEpoch"/> means <see cref="Clear"/> already
  /// abandoned the lease.
  /// </summary>
  internal static Task EndRunLeaseAsync(
    long clearEpoch,
    CancellationToken cancellationToken = default)
  {
    List<(Type Type, IAsyncDisposable Instance)>? toDispose = DecideRunEnd(clearEpoch);
    if (toDispose is null)
    {
      return Task.CompletedTask;
    }

    return DisposeCreatedAsync(toDispose, cancellationToken);
  }

  private static Task EndHostAsync(long? clearEpoch, CancellationToken cancellationToken)
  {
    List<(Type Type, IAsyncDisposable Instance)>? toDispose = DecideHostEnd(clearEpoch);
    if (toDispose is null)
    {
      return Task.CompletedTask;
    }

    return DisposeCreatedAsync(toDispose, cancellationToken);
  }

  private static List<(Type Type, IAsyncDisposable Instance)>? DecideHostEnd(long? clearEpoch)
  {
    lock (RegistryLock)
    {
      if (clearEpoch is long epoch && epoch != ClearEpoch)
      {
        return null;
      }

      if (HostDepth <= 0)
      {
        throw new InvalidOperationException(
          "EndTestSessionAsync was called with no active host session. " +
          "BeginTestSession and EndTestSessionAsync must be balanced. " +
          "ClearRegisteredSessionFixtures resets host depth and run depth for the process, " +
          "drops registrations, and does not dispose. It throws when an instance is still live.");
      }

      HostDepth--;
      if (HostDepth != 0 || RunDepth != 0)
      {
        return null;
      }

      return TakeDisposeList();
    }
  }

  private static List<(Type Type, IAsyncDisposable Instance)>? DecideRunEnd(long clearEpoch)
  {
    lock (RegistryLock)
    {
      if (clearEpoch != ClearEpoch)
      {
        return null;
      }

      if (RunDepth <= 0)
      {
        throw new InvalidOperationException(
          "Run session lease ended with no active run lease.");
      }

      RunDepth--;
      if (HostDepth != 0 || RunDepth != 0)
      {
        return null;
      }

      return TakeDisposeList();
    }
  }

  /// <summary>
  /// Caller holds <see cref="RegistryLock"/>.
  /// </summary>
  private static List<(Type Type, IAsyncDisposable Instance)> TakeDisposeList()
  {
    Generation++;
    List<(Type Type, IAsyncDisposable Instance)> toDispose = [with(capacity: CreationOrder.Count)];
    for (int index = CreationOrder.Count - 1; index >= 0; index--)
    {
      Type type = CreationOrder[index];
      if (!Registry.TryGetValue(type, out FixtureEntry? entry))
      {
        continue;
      }

      if (entry.Instance is IAsyncDisposable disposable)
      {
        toDispose.Add((type, disposable));
        entry.Instance = null;
      }
    }

    CreationOrder.Clear();

    foreach (KeyValuePair<Type, FixtureEntry> pair in Registry)
    {
      // Sticky create failures are session-scoped; clear when the session fully ends.
      pair.Value.CreateFailure = null;
      if (pair.Value.Instance is IAsyncDisposable disposable)
      {
        toDispose.Add((pair.Key, disposable));
        pair.Value.Instance = null;
      }
    }

    return toDispose;
  }

  private static async Task DisposeCreatedAsync(
    List<(Type Type, IAsyncDisposable Instance)> toDispose,
    CancellationToken cancellationToken)
  {
    List<Exception> errors = [];
    List<(Type Type, Task DisposeTask)> pending = [with(capacity: toDispose.Count)];
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
