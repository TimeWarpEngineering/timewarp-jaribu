namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Runtime.ExceptionServices;

#region Purpose
// Session-scoped fixture registry: explicit RegisterSessionFixture + lazy
// SessionFixture.GetAsync, disposed when the outermost session ends.
// Amortizes expensive fixtures (e.g. DistributedApplication) across classes.
#endregion

#region Design
// Host depth (BeginSession / End) and run leases (lone RunTestsAsync) share one
// lock. A run lease is taken only when host depth is zero, inside that lock.
// Overlapping lone runs each take a lease. Dispose starts when both depths hit
// zero, in reverse creation order. Registrations survive across sessions.
//
// Clear() is the process-wide meta-test reset. It drops registrations, zeroes
// both depths, and bumps the clear epoch and the create generation. It does not
// dispose, and it throws while an instance is live. A host end that captured
// the epoch from before Clear returns without throwing. Public hosts pass that
// epoch to EndTestSessionAsync(long). The parameterless EndTestSessionAsync
// throws when host depth is already zero.
//
// Create is lazy on first GetAsync per type within a session (single-flight via
// per-type SemaphoreSlim). Classes that never call GetAsync never create.
// Failed CreateAsync is sticky for the rest of the session (rethrow same
// exception; no second create attempt) so partial boots do not retry.
// A create timeout is not sticky. It used the caller class's hook budget,
// so a later class retries under its own budget or with none.
//
// A generation counter guards the create/end race: create holds only the
// per-type gate, so a session can fully end while CreateAsync is in flight
// (e.g. a [Timeout]-abandoned task). If the generation changed by store time,
// the orphan instance is disposed and GetAsync throws instead of caching a
// fixture that belongs to a dead session.
//
// Authors must not dispose session fixtures in CleanUpOnce — the session owns
// dispose. CleanUpOnce may null local references only.
//
// Create and dispose observe the caller token, or the run's ambient token when
// GetAsync is called without one. A cancelled wait does not cache the fixture.
// The in-flight task is observed, and a late instance is disposed.
// A cancelled end starts dispose for every created fixture, observes each task,
// and then throws, so a later fixture is not detached and leaked.
// CreateAsync uses the ambient hook timeout. A timeout or a create fault is
// marked so the test node stays Timeout or Error. The timeout is not stored
// on the entry. A late instance is disposed. A wrong returned instance and an
// orphan from a session that ended during create are fixture-creation errors.
//
// MTP: CreateTestSessionAsync → Begin (keeps the epoch); Close → End(epoch).
// Standalone: RunAllTests wraps begin/end; lone RunTestsAsync takes a run lease
// when no host session is active. Inner runs under a host do not.
#endregion

/// <summary>
/// Access point for session-scoped fixtures registered via
/// <see cref="TestRunner.RegisterSessionFixture{T}"/>.
/// Resolve with <see cref="GetAsync{T}"/> from SetupOnce (or tests); the session owns disposal.
/// </summary>
public static partial class SessionFixture
{
  private static readonly Lock RegistryLock = new();
  private static readonly Dictionary<Type, FixtureEntry> Registry = [];
  private static readonly List<Type> CreationOrder = [];
  private static int HostDepth;
  private static int RunDepth;
  private static long ClearEpoch;
  private static int Generation;

  /// <summary>
  /// Caller holds <see cref="RegistryLock"/>.
  /// </summary>
  private static bool SessionIsOpen()
  {
    return HostDepth > 0 || RunDepth > 0;
  }

  /// <summary>
  /// Lazily creates (or returns) the session fixture of type <typeparamref name="T"/>.
  /// Must be called while a test session is active.
  /// </summary>
  /// <typeparam name="T">Fixture type registered via <see cref="TestRunner.RegisterSessionFixture{T}"/>.</typeparam>
  /// <param name="cancellationToken">
  /// Stops waiting for creation. When this token cannot be cancelled, the active run's token is used.
  /// </param>
  /// <returns>The shared fixture instance for the current session.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no session is active, the type is not registered, or create fails.
  /// </exception>
  /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> fires.</exception>
  public static async Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
    where T : class, IAsyncDisposable
  {
    CancellationToken effectiveToken = cancellationToken.CanBeCanceled
      ? cancellationToken
      : TestRunner.AmbientCancellationToken;
    Type fixtureType = typeof(T);
    FixtureEntry entry;
    int generation;

    lock (RegistryLock)
    {
      generation = Generation;
      if (!SessionIsOpen())
      {
        throw new InvalidOperationException(
          $"No active test session. SessionFixture.GetAsync<{fixtureType.Name}>() " +
          "requires BeginTestSession / RunTestsAsync / RunAllTests / MTP session.");
      }

      if (!Registry.TryGetValue(fixtureType, out FixtureEntry? found))
      {
        throw new InvalidOperationException(
          $"Session fixture '{fixtureType.Name}' is not registered.\n" +
          $"Call TestRunner.RegisterSessionFixture<{fixtureType.Name}>() from a [ModuleInitializer]\n" +
          "alongside RegisterTests. See Jaribu session fixtures docs.");
      }

      entry = found;
      if (entry.Instance is T existing)
      {
        return existing;
      }

      if (entry.CreateFailure is not null)
      {
        ExceptionDispatchInfo.Capture(entry.CreateFailure).Throw();
      }
    }

    await entry.CreateGate.WaitAsync(effectiveToken).ConfigureAwait(false);
    try
    {
      lock (RegistryLock)
      {
        if (entry.Instance is T existing)
        {
          return existing;
        }

        if (entry.CreateFailure is not null)
        {
          ExceptionDispatchInfo.Capture(entry.CreateFailure).Throw();
        }
      }

      try
      {
        object created = await InvokeCreateAsync(entry.Factory, effectiveToken).ConfigureAwait(false);
        if (created is not T typed)
        {
          throw FailureMark.Mark(
            new InvalidOperationException(
              $"Session fixture '{fixtureType.Name}' CreateAsync returned " +
              $"{created?.GetType().FullName ?? "null"}, expected {fixtureType.FullName}."),
            FailureMark.FixtureCreation);
        }

        bool sessionStillCurrent;
        lock (RegistryLock)
        {
          sessionStillCurrent = SessionIsOpen() && Generation == generation;
          if (sessionStillCurrent)
          {
            entry.Instance = typed;
            entry.CreateFailure = null;
            CreationOrder.Add(fixtureType);
          }
        }

        if (!sessionStillCurrent)
        {
          // The session ended (or a new one began) while CreateAsync was in
          // flight; the instance belongs to a dead session — dispose the orphan.
          await typed.DisposeAsync().ConfigureAwait(false);
          throw FailureMark.Mark(
            new InvalidOperationException(
              $"Test session ended while session fixture '{fixtureType.Name}' was being created. " +
              "The orphaned instance was disposed."),
            FailureMark.FixtureCreation);
        }

        return typed;
      }
      catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception ex)
      {
        lock (RegistryLock)
        {
          // Sticky failures are per-session; never record one against a session
          // that ended (or was replaced) while create was in flight.
          // A timeout belongs to the caller class's budget, so a later class retries.
          if (SessionIsOpen() &&
              Generation == generation &&
              !FailureMark.Is(ex, FailureMark.FixtureCreationTimeout))
          {
            entry.CreateFailure = ex;
          }
        }

        throw;
      }
    }
    finally
    {
      entry.CreateGate.Release();
    }
  }

  /// <summary>
  /// Validates and registers a fixture type. Fail-fast on double-register or bad CreateAsync.
  /// </summary>
  internal static void Register(Type fixtureType)
  {
    ArgumentNullException.ThrowIfNull(fixtureType);

    if (!typeof(IAsyncDisposable).IsAssignableFrom(fixtureType))
    {
      throw new InvalidOperationException(
        $"Session fixture '{fixtureType.FullName}' must implement IAsyncDisposable.");
    }

    if (fixtureType.IsAbstract || fixtureType.IsInterface)
    {
      throw new InvalidOperationException(
        $"Session fixture '{fixtureType.FullName}' must be a concrete class.");
    }

    MethodInfo factory = ResolveCreateFactory(fixtureType);

    lock (RegistryLock)
    {
      if (Registry.ContainsKey(fixtureType))
      {
        throw new InvalidOperationException(
          $"Session fixture '{fixtureType.FullName}' is already registered. " +
          "Double-registration is not allowed; register once from a [ModuleInitializer].");
      }

      Registry[fixtureType] = new FixtureEntry(factory);
    }
  }

  /// <summary>
  /// Drops every registration and zeroes host depth and run depth.
  /// This is a process-wide reset for meta-tests. It does not dispose.
  /// A live instance throws so the caller ends the session first.
  /// The clear epoch and the create generation both advance, so an in-flight
  /// create cannot join the next session, and a host end that captured the
  /// previous epoch returns without throwing or disposing.
  /// <see cref="TestRunner.EndTestSessionAsync(long, CancellationToken)"/> is that
  /// captured-epoch end. <see cref="TestRunner.EndTestSessionAsync(CancellationToken)"/>
  /// has no captured epoch and throws when host depth is zero, including after this reset.
  /// </summary>
  /// <exception cref="InvalidOperationException">A created fixture instance is still live.</exception>
  internal static void Clear()
  {
    lock (RegistryLock)
    {
      foreach (KeyValuePair<Type, FixtureEntry> pair in Registry)
      {
        if (pair.Value.Instance is not null)
        {
          throw new InvalidOperationException(
            $"Cannot clear session fixtures: '{pair.Key.FullName}' has a live instance. " +
            "End the session first (EndTestSessionAsync) so it is disposed.");
        }
      }

      foreach (FixtureEntry entry in Registry.Values)
      {
        entry.CreateFailure = null;
      }

      Registry.Clear();
      CreationOrder.Clear();
      HostDepth = 0;
      RunDepth = 0;
      ClearEpoch++;
      Generation++;
    }
  }

  /// <summary>
  /// Begins a host session. Nested begins each add one level of host depth.
  /// The returned epoch lets the matching end ignore a later <see cref="Clear"/>.
  /// </summary>
  internal static long BeginSession()
  {
    lock (RegistryLock)
    {
      HostDepth++;
      return ClearEpoch;
    }
  }

  /// <summary>
  /// Pins a run lease when no host session is active.
  /// Returns false when a host session is active; the caller must not end it.
  /// Overlapping lone runs each receive a lease and each must end.
  /// The check and the pin share <see cref="RegistryLock"/>.
  /// </summary>
  internal static bool TryBeginRunLease(out long clearEpoch)
  {
    lock (RegistryLock)
    {
      clearEpoch = ClearEpoch;
      if (HostDepth > 0)
      {
        return false;
      }

      RunDepth++;
      return true;
    }
  }

  private static MethodInfo ResolveCreateFactory(Type fixtureType)
  {
    const BindingFlags Flags =
      BindingFlags.Public |
      BindingFlags.NonPublic |
      BindingFlags.Static |
      BindingFlags.Instance |
      BindingFlags.DeclaredOnly;

    List<MethodInfo> named = [];
    Type? current = fixtureType;
    while (current is not null && current != typeof(object))
    {
      foreach (MethodInfo method in current.GetMethods(Flags))
      {
        if (method.Name == "CreateAsync")
        {
          named.Add(method);
        }
      }

      if (named.Count > 0)
      {
        break;
      }

      current = current.BaseType;
    }

    if (named.Count == 0)
    {
      throw new InvalidOperationException(
        $"Session fixture '{fixtureType.FullName}' must declare " +
        $"'public static Task<{fixtureType.Name}> CreateAsync()' (parameterless).");
    }

    if (named.Count > 1)
    {
      throw new InvalidOperationException(
        $"Session fixture '{fixtureType.FullName}' has multiple methods named 'CreateAsync'. " +
        "Exactly one 'public static Task<T> CreateAsync()' is required.");
    }

    MethodInfo candidate = named[0];
    Type expectedReturn = typeof(Task<>).MakeGenericType(fixtureType);

    if (!candidate.IsPublic ||
        !candidate.IsStatic ||
        candidate.IsGenericMethodDefinition ||
        candidate.GetParameters().Length != 0 ||
        candidate.ReturnType != expectedReturn)
    {
      throw new InvalidOperationException(
        $"Session fixture '{fixtureType.FullName}' method 'CreateAsync' must be " +
        $"'public static Task<{fixtureType.Name}> CreateAsync()' " +
        "(parameterless, non-generic). Non-conforming signatures fail registration.");
    }

    return candidate;
  }

  private static async Task<object> InvokeCreateAsync(MethodInfo factory, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    string site = factory.DeclaringType?.Name ?? "session fixture";
    object? invoked;
    try
    {
      invoked = factory.Invoke(null, null);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      throw FailureMark.Mark(ex.InnerException, FailureMark.FixtureCreation);
    }

    if (invoked is not Task task)
    {
      throw FailureMark.Mark(
        new InvalidOperationException(
          $"CreateAsync on '{factory.DeclaringType?.FullName}' did not return a Task."),
        FailureMark.FixtureCreation);
    }

    try
    {
      TestRunner.WaitKind kind = await TestRunner.WaitForCompletionAsync(
        task,
        TestRunner.CurrentHookTimeoutMilliseconds,
        cancellationToken,
        disposeAbandonedResult: true).ConfigureAwait(false);
      if (kind == TestRunner.WaitKind.Cancelled)
      {
        throw new OperationCanceledException(cancellationToken);
      }

      if (kind == TestRunner.WaitKind.TimedOut)
      {
        int milliseconds = TestRunner.CurrentHookTimeoutMilliseconds ?? 0;
        throw FailureMark.Mark(
          new TimeoutException(TestRunner.TimeoutAfter(milliseconds, $"session fixture '{site}' CreateAsync")),
          FailureMark.FixtureCreationTimeout);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception ex) when (FailureMark.Is(ex, FailureMark.FixtureCreationTimeout))
    {
      throw;
    }
    catch (Exception ex) when (FailureMark.Is(ex, FailureMark.FixtureCreation))
    {
      throw;
    }
    catch (Exception ex)
    {
      throw FailureMark.Mark(ex, FailureMark.FixtureCreation);
    }

    PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
    if (resultProperty is null)
    {
      throw FailureMark.Mark(
        new InvalidOperationException(
          $"CreateAsync on '{factory.DeclaringType?.FullName}' did not return Task<T>."),
        FailureMark.FixtureCreation);
    }

    object? result = resultProperty.GetValue(task);
    if (result is null)
    {
      throw FailureMark.Mark(
        new InvalidOperationException(
          $"CreateAsync on '{factory.DeclaringType?.FullName}' returned a null fixture instance."),
        FailureMark.FixtureCreation);
    }

    return result;
  }

  private sealed class FixtureEntry
  {
    public FixtureEntry(MethodInfo factory)
    {
      Factory = factory;
    }

    public MethodInfo Factory { get; }
    public object? Instance { get; set; }
    public Exception? CreateFailure { get; set; }
    public SemaphoreSlim CreateGate { get; } = new(1, 1);
  }
}
