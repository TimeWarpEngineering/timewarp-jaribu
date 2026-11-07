namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Runtime.ExceptionServices;

#region Purpose
// Session-scoped fixture registry: explicit RegisterSessionFixture + lazy
// SessionFixture.GetAsync, disposed when the outermost session ends.
// Amortizes expensive fixtures (e.g. DistributedApplication) across classes.
#endregion

#region Design
// Nesting counter: BeginSession/EndSessionAsync. Dispose all created instances
// only when nesting hits 0; registrations survive across sessions.
//
// Create is lazy on first GetAsync per type within a session (single-flight via
// per-type SemaphoreSlim). Classes that never call GetAsync never create.
// Failed CreateAsync is sticky for the rest of the session (rethrow same
// exception; no second create attempt) so partial boots do not retry.
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
// marked so the test node stays Timeout or Error. A late instance is disposed.
//
// MTP: CreateTestSessionAsync → Begin; CloseTestSessionAsync → End.
// Standalone: RunAllTests wraps begin/end; lone RunTestsAsync owns session-of-one.
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
  private static int Nesting;
  private static int Generation;

  /// <summary>
  /// True when at least one session has been begun and not yet fully ended.
  /// </summary>
  internal static bool IsSessionActive
  {
    get
    {
      lock (RegistryLock)
      {
        return Nesting > 0;
      }
    }
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
      if (Nesting <= 0)
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
          throw new InvalidOperationException(
            $"Session fixture '{fixtureType.Name}' CreateAsync returned " +
            $"{created?.GetType().FullName ?? "null"}, expected {fixtureType.FullName}.");
        }

        bool sessionStillCurrent;
        lock (RegistryLock)
        {
          sessionStillCurrent = Nesting > 0 && Generation == generation;
          if (sessionStillCurrent)
          {
            entry.Instance = typed;
            entry.CreateFailure = null;
          }
        }

        if (!sessionStillCurrent)
        {
          // The session ended (or a new one began) while CreateAsync was in
          // flight; the instance belongs to a dead session — dispose the orphan.
          await typed.DisposeAsync().ConfigureAwait(false);
          throw new InvalidOperationException(
            $"Test session ended while session fixture '{fixtureType.Name}' was being created. " +
            "The orphaned instance was disposed.");
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
          if (Nesting > 0 && Generation == generation)
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
  /// Clears all registrations and resets session nesting to zero — a full
  /// reset-the-world escape hatch for meta-tests. Throws when any fixture
  /// instance is still live (that would silently leak an undisposed fixture);
  /// end the session first so instances are disposed.
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
      Nesting = 0;
      Generation++;
    }
  }

  /// <summary>
  /// Begins a (possibly nested) test session.
  /// </summary>
  internal static void BeginSession()
  {
    lock (RegistryLock)
    {
      Nesting++;
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
