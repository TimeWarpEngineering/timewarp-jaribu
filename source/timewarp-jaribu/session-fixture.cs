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
// Authors must not dispose session fixtures in CleanUpOnce — the session owns
// dispose. CleanUpOnce may null local references only.
//
// MTP: CreateTestSessionAsync → Begin; CloseTestSessionAsync → End.
// Standalone: RunAllTests wraps begin/end; lone RunTestsAsync owns session-of-one.
#endregion

/// <summary>
/// Access point for session-scoped fixtures registered via
/// <see cref="TestRunner.RegisterSessionFixture{T}"/>.
/// Resolve with <see cref="GetAsync{T}"/> from SetupOnce (or tests); the session owns disposal.
/// </summary>
public static class SessionFixture
{
  private static readonly Lock RegistryLock = new();
  private static readonly Dictionary<Type, FixtureEntry> Registry = [];
  private static int Nesting;

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
  /// <returns>The shared fixture instance for the current session.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no session is active, the type is not registered, or create fails.
  /// </exception>
  public static async Task<T> GetAsync<T>() where T : class, IAsyncDisposable
  {
    Type fixtureType = typeof(T);
    FixtureEntry entry;

    lock (RegistryLock)
    {
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

    await entry.CreateGate.WaitAsync().ConfigureAwait(false);
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
        object created = await InvokeCreateAsync(entry.Factory).ConfigureAwait(false);
        if (created is not T typed)
        {
          throw new InvalidOperationException(
            $"Session fixture '{fixtureType.Name}' CreateAsync returned " +
            $"{created?.GetType().FullName ?? "null"}, expected {fixtureType.FullName}.");
        }

        lock (RegistryLock)
        {
          entry.Instance = typed;
          entry.CreateFailure = null;
        }

        return typed;
      }
      catch (Exception ex)
      {
        lock (RegistryLock)
        {
          entry.CreateFailure = ex;
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
  /// Clears all registrations and live instance references (does not dispose).
  /// Intended for meta-tests; prefer ending the session first when instances exist.
  /// </summary>
  internal static void Clear()
  {
    lock (RegistryLock)
    {
      foreach (FixtureEntry entry in Registry.Values)
      {
        entry.Instance = null;
        entry.CreateFailure = null;
      }

      Registry.Clear();
      Nesting = 0;
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

  /// <summary>
  /// Ends a test session. When nesting reaches zero, disposes all created fixtures
  /// and clears instance references (registrations are kept).
  /// </summary>
  internal static async Task EndSessionAsync()
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
    foreach ((Type type, IAsyncDisposable instance) in toDispose)
    {
      try
      {
        await instance.DisposeAsync().ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        errors.Add(new InvalidOperationException(
          $"Session fixture '{type.FullName}' DisposeAsync failed: {ex.Message}",
          ex));
      }
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

  private static async Task<object> InvokeCreateAsync(MethodInfo factory)
  {
    object? invoked;
    try
    {
      invoked = factory.Invoke(null, null);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
      throw; // unreachable
    }

    if (invoked is not Task task)
    {
      throw new InvalidOperationException(
        $"CreateAsync on '{factory.DeclaringType?.FullName}' did not return a Task.");
    }

    try
    {
      await task.ConfigureAwait(false);
    }
    catch (Exception)
    {
      throw;
    }

    PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
    if (resultProperty is null)
    {
      throw new InvalidOperationException(
        $"CreateAsync on '{factory.DeclaringType?.FullName}' did not return Task<T>.");
    }

    object? result = resultProperty.GetValue(task);
    if (result is null)
    {
      throw new InvalidOperationException(
        $"CreateAsync on '{factory.DeclaringType?.FullName}' returned a null fixture instance.");
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
