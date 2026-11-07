namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Runtime.ExceptionServices;
using TimeWarp.Terminal;

#region Purpose
// Resolves and invokes per-test Setup/CleanUp and class-scoped SetupOnce/CleanUpOnce.
#endregion

#region Design
// Hook methods stay parameterless. The runner stops waiting when the caller
// token fires. A cancellation during SetupOnce still pairs with CleanUpOnce
// once SetupOnce has been entered. Cancellation is not stored as a hook failure.
// CleanUp and CleanUpOnce may overlap a cancelled body or SetupOnce that is
// still running. Abandoned hook tasks are observed.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// CleanUp outcome. <see cref="Cancelled"/> means the caller token fired.
  /// That is not a cleanup failure.
  /// </summary>
  private readonly record struct CleanupOutcome(Exception? Failure, bool Cancelled);

  /// <summary>
  /// Invokes the Setup method for a given type if it exists.
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
  /// </summary>
  private static async Task<WaitKind> InvokeSetupForType(Type testClass, CancellationToken cancellationToken)
  {
    MethodInfo? setupMethod = testClass.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
    if (setupMethod is null || setupMethod.ReturnType != typeof(Task))
    {
      return WaitKind.Completed;
    }

    if (setupMethod.Invoke(null, null) is not Task task)
    {
      return WaitKind.Completed;
    }

    return await WaitForCompletionAsync(task, timeoutMilliseconds: null, cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Invokes the CleanUp method for a given type if it exists.
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
  /// </summary>
  private static async Task<WaitKind> InvokeCleanupForType(Type testClass, CancellationToken cancellationToken)
  {
    MethodInfo? cleanupMethod = testClass.GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod is null || cleanupMethod.ReturnType != typeof(Task))
    {
      return WaitKind.Completed;
    }

    if (cleanupMethod.Invoke(null, null) is not Task task)
    {
      return WaitKind.Completed;
    }

    return await WaitForCompletionAsync(task, timeoutMilliseconds: null, cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Runs CleanUp without letting its exception escape. Failure is null when cleanup
  /// succeeds, no conforming CleanUp exists, or the caller token cancelled the wait.
  /// </summary>
  private static async Task<CleanupOutcome> InvokeCleanupIsolatedAsync(
    Type testClass,
    CancellationToken cancellationToken)
  {
    try
    {
      WaitKind kind = await InvokeCleanupForType(testClass, cancellationToken).ConfigureAwait(false);
      return new CleanupOutcome(Failure: null, Cancelled: kind == WaitKind.Cancelled);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        return new CleanupOutcome(Failure: null, Cancelled: true);
      }

      return new CleanupOutcome(ex.InnerException, Cancelled: false);
    }
#pragma warning disable CA1031 // Cleanup failure is data for the node, not a crash
    catch (Exception ex)
#pragma warning restore CA1031
    {
      if (cancellationToken.IsCancellationRequested)
      {
        return new CleanupOutcome(Failure: null, Cancelled: true);
      }

      return new CleanupOutcome(ex, Cancelled: false);
    }
  }

  /// <summary>
  /// Prefers the body outcome. A passing body becomes Error. Any other state keeps its
  /// exception and state, and the cleanup failure is appended to the message.
  /// Both messages name CleanUp so a passing body is not mistaken for a body failure.
  /// </summary>
  private static TestNodeInfo FoldCleanupFailure(TestNodeInfo outcome, Exception cleanupException)
  {
    string cleanupText = $"{cleanupException.GetType().Name}: {cleanupException.Message}";
    if (outcome.State is TestNodeState.Passed)
    {
      return outcome with
      {
        State = TestNodeState.Error,
        Exception = cleanupException,
        Message = $"CleanUp {cleanupText}"
      };
    }

    string bodyText = outcome.Message ?? outcome.Exception?.Message ?? outcome.State.ToString();
    return outcome with
    {
      Message = $"{bodyText}; CleanUp {cleanupText}"
    };
  }

  /// <summary>
  /// Ends an owned session. Returns the dispose exception instead of throwing.
  /// </summary>
  private static async Task<Exception?> TryEndOwnedSessionAsync(CancellationToken cancellationToken)
  {
    try
    {
      await SessionFixture.EndSessionAsync(cancellationToken).ConfigureAwait(false);
      return null;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      return null;
    }
#pragma warning disable CA1031 // Host boundary: dispose failure becomes a failed node
    catch (Exception ex)
#pragma warning restore CA1031
    {
      return ex;
    }
  }

  /// <summary>
  /// Ends an owned session while another exception is already in flight.
  /// The dispose exception is logged and swallowed so it cannot replace the body exception.
  /// Cancellation is not a dispose failure.
  /// </summary>
  private static async Task EndOwnedSessionWithoutMaskingAsync(CancellationToken cancellationToken)
  {
    try
    {
      await SessionFixture.EndSessionAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
#pragma warning disable CA1031 // Host boundary: do not replace an in-flight exception
    catch (Exception ex)
#pragma warning restore CA1031
    {
#pragma warning disable CA1849 // Same host-boundary log as RunAllTests
      Terminal.WriteLine($"✗ Session fixture dispose failed: {ex.Message}");
#pragma warning restore CA1849
    }
  }

  /// <summary>
  /// Emits a synthetic failed node for a class-hook failure (e.g. CleanUpOnce)
  /// or a session-fixture dispose failure.
  /// </summary>
  private static async Task EmitSyntheticHookFailureAsync(
    Type testClass,
    string hookName,
    Exception exception,
    ITestResultSink sink,
    List<TestNodeInfo> results,
    string? message = null)
  {
    string uid = $"{testClass.FullName}.{hookName}";
    string nodeMessage = message ?? $"{exception.GetType().Name}: {exception.Message}";
    MethodInfo? hook = testClass.GetMethod(hookName, BindingFlags.Public | BindingFlags.Static);
    TestNodeInfo failedNode = new(
      uid,
      hookName,
      TestNodeState.Failed,
      TimeSpan.Zero,
      Exception: exception,
      Message: nodeMessage,
      Parameters: null,
      Method: hook
    );
    await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
    await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
    results.Add(failedNode);
  }

  /// <summary>
  /// Mutable per-class state for lazy SetupOnce / CleanUpOnce invocation.
  /// </summary>
  private sealed class ClassOnceState
  {
    public MethodInfo? SetupOnce { get; init; }
    public MethodInfo? CleanUpOnce { get; init; }
    public bool SetupOnceInvoked { get; set; }
    public Exception? SetupOnceFailure { get; set; }
  }

  /// <summary>
  /// Result of resolving a class-scoped once-hook by name.
  /// Method set and ValidationError null means no hook. ValidationError set means fail-fast.
  /// </summary>
  private readonly record struct OnceHookResolveResult(MethodInfo? Method, Exception? ValidationError);

  /// <summary>
  /// Resolves a class-scoped once hook. Fail-fast on near-miss signatures; never silent-ignore.
  /// Prefers the most-derived declaring type when base types also define the name.
  /// </summary>
  private static OnceHookResolveResult ResolveOnceHook(Type testClass, string name)
  {
    const BindingFlags Flags =
      BindingFlags.Public |
      BindingFlags.NonPublic |
      BindingFlags.Static |
      BindingFlags.Instance |
      BindingFlags.DeclaredOnly;

    // Walk from the registered test class toward object; first type that declares
    // the name wins (most-derived preference). Overloads on that type are errors.
    Type? current = testClass;
    while (current is not null && current != typeof(object))
    {
      List<MethodInfo> candidates = [];
      foreach (MethodInfo method in current.GetMethods(Flags))
      {
        if (method.Name == name)
        {
          candidates.Add(method);
        }
      }

      if (candidates.Count > 1)
      {
        return new OnceHookResolveResult(
          Method: null,
          ValidationError: new InvalidOperationException(
            $"Type '{current.FullName}' has multiple methods named '{name}'. " +
            "Class-scoped hooks must have exactly one method."));
      }

      if (candidates.Count == 1)
      {
        MethodInfo candidate = candidates[0];
        if (!IsValidOnceHookSignature(candidate))
        {
          return new OnceHookResolveResult(
            Method: null,
            ValidationError: new InvalidOperationException(
              $"Type '{testClass.FullName}' method '{name}' must be 'public static Task {name}()' " +
              "(parameterless, non-generic). Non-conforming signatures fail the class."));
        }

        return new OnceHookResolveResult(Method: candidate, ValidationError: null);
      }

      current = current.BaseType;
    }

    return new OnceHookResolveResult(Method: null, ValidationError: null);
  }

  private static bool IsValidOnceHookSignature(MethodInfo method)
  {
    return method.IsPublic &&
      method.IsStatic &&
      method.ReturnType == typeof(Task) &&
      !method.IsGenericMethodDefinition &&
      method.GetParameters().Length == 0;
  }

  /// <summary>
  /// Invokes a resolved once-hook and awaits its Task, unwrapping TargetInvocationException.
  /// Returns <see cref="WaitKind.Cancelled"/> when the caller token fires.
  /// </summary>
  private static async Task<WaitKind> InvokeOnceHookAsync(MethodInfo method, CancellationToken cancellationToken)
  {
    object? result;
    try
    {
      result = method.Invoke(null, null);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
      throw; // unreachable; satisfies definite-assignment analysis
    }

    if (result is not Task task)
    {
      return WaitKind.Completed;
    }

    return await WaitForCompletionAsync(task, timeoutMilliseconds: null, cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Idempotent: invokes SetupOnce on first call. Sets SetupOnceInvoked before await
  /// so a throwing or cancelled SetupOnce still pairs with CleanUpOnce.
  /// Returns true when the caller token cancelled the hook before any test runs.
  /// </summary>
  private static async Task<bool> EnsureSetupOnceAsync(ClassOnceState state, CancellationToken cancellationToken)
  {
    if (state.SetupOnceInvoked || state.SetupOnce is null)
    {
      return cancellationToken.IsCancellationRequested;
    }

    if (cancellationToken.IsCancellationRequested)
    {
      return true;
    }

    state.SetupOnceInvoked = true;
    try
    {
      WaitKind kind = await InvokeOnceHookAsync(state.SetupOnce, cancellationToken).ConfigureAwait(false);
      return kind == WaitKind.Cancelled;
    }
    catch (Exception) when (cancellationToken.IsCancellationRequested)
    {
      return true;
    }
    catch (Exception ex)
    {
      state.SetupOnceFailure = ex;
      return false;
    }
  }

  /// <summary>
  /// Reports every selected case for a method as Failed due to a class hook error.
  /// </summary>
  private static async Task ReportCasesFailedDueToHookAsync(
    IReadOnlyList<ReportedTestCase> cases,
    Exception hookException,
    ITestResultSink sink,
    List<TestNodeInfo> results)
  {
    string message = $"{hookException.GetType().Name}: {hookException.Message}";
    foreach (ReportedTestCase testCase in cases)
    {
      TestNodeInfo failedNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.Failed,
        TimeSpan.Zero,
        Exception: hookException,
        Message: message,
        Parameters: testCase.Parameters,
        Method: testCase.Method);
      await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
      await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
      results.Add(failedNode);
    }
  }
}
