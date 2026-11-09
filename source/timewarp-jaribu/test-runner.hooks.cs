namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

#region Purpose
// Resolves and invokes per-test Setup/CleanUp and class-scoped SetupOnce/CleanUpOnce.
#endregion

#region Design
// Hook methods stay parameterless. The runner stops waiting when the caller
// token fires. A cancellation during SetupOnce still pairs with CleanUpOnce
// once SetupOnce has been entered. Cancellation is not stored as a hook failure.
// CleanUp and CleanUpOnce may overlap a cancelled body or SetupOnce that is
// still running. Abandoned hook tasks are observed. Hook waits use the class
// TimeoutAttribute or DefaultHookTimeout. Setup, CleanUp, SetupOnce, and
// CleanUpOnce are cached per class. A near-miss hook fails the class
// before any body runs. A throw from Invoke is an invoke failure, and a fault
// from the Setup task is a body failure. A CleanUpOnce that throws before it
// returns a task is rethrown here. The class run traces that throw once the
// caller token has fired, and reports it as a synthetic node otherwise.
// A near-miss node is given the MethodInfo the diagnostic already holds.
// NoTests and HookSignature do not look a method up by those names.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// CleanUp outcome. <see cref="Cancelled"/> means the caller token fired.
  /// That is not a cleanup failure.
  /// </summary>
  private readonly record struct CleanupOutcome(Exception? Failure, bool Cancelled);

  /// <summary>
  /// A started Setup task. <see cref="Task"/> is null when the type has no Setup.
  /// </summary>
  private readonly record struct SetupStart(Task? Task);

  /// <summary>
  /// Invokes a Setup method already checked by <see cref="ResolveHook"/>.
  /// Null means the type has no Setup. A throw from the method escapes to the caller.
  /// </summary>
  private static SetupStart StartSetup(MethodInfo? setupMethod)
  {
    if (setupMethod is null)
    {
      return new SetupStart(null);
    }

    if (setupMethod.Invoke(null, null) is not Task task)
    {
      throw new InvalidOperationException(
        $"Method '{setupMethod.DeclaringType?.FullName}.{setupMethod.Name}' did not return Task.");
    }

    return new SetupStart(task);
  }

  /// <summary>
  /// Invokes a CleanUp method already checked by <see cref="ResolveHook"/>.
  /// Null means the type has no CleanUp.
  /// </summary>
  private static async Task<WaitKind> InvokeCleanupForType(
    Type testClass,
    MethodInfo? cleanupMethod,
    CancellationToken cancellationToken)
  {
    if (cleanupMethod is null)
    {
      return WaitKind.Completed;
    }

    if (cleanupMethod.Invoke(null, null) is not Task task)
    {
      throw new InvalidOperationException(
        $"Method '{cleanupMethod.DeclaringType?.FullName}.{cleanupMethod.Name}' did not return Task.");
    }

    return await WaitForCompletionAsync(
      task,
      ResolveHookTimeoutMilliseconds(testClass),
      cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Runs CleanUp without letting its exception escape. Failure is null when cleanup
  /// succeeds, no conforming CleanUp exists, or the caller token cancelled the wait.
  /// </summary>
  private static async Task<CleanupOutcome> InvokeCleanupIsolatedAsync(
    Type testClass,
    MethodInfo? cleanupMethod,
    CancellationToken cancellationToken)
  {
    try
    {
      WaitKind kind = await InvokeCleanupForType(testClass, cleanupMethod, cancellationToken)
        .ConfigureAwait(false);
      if (kind == WaitKind.TimedOut)
      {
        int milliseconds = ResolveHookTimeoutMilliseconds(testClass) ?? 0;
        return new CleanupOutcome(
          FailureMark.Mark(
            new TimeoutException(TimeoutAfter(milliseconds, "CleanUp")),
            FailureMark.HookTimeout),
          Cancelled: false);
      }

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
  /// Prefers the body outcome. A passing body becomes Error, or Timeout when CleanUp
  /// timed out. Any other state keeps its exception and state, and the cleanup failure
  /// is appended to the message. Both messages name CleanUp so a passing body is not
  /// mistaken for a body failure.
  /// </summary>
  private static TestNodeInfo FoldCleanupFailure(TestNodeInfo outcome, Exception cleanupException)
  {
    string cleanupText = $"{cleanupException.GetType().Name}: {cleanupException.Message}";
    if (outcome.State is TestNodeState.Passed &&
        FailureMark.Is(cleanupException, FailureMark.HookTimeout))
    {
      return outcome with
      {
        State = TestNodeState.Timeout,
        Exception = cleanupException,
        Message = $"CleanUp {cleanupText}"
      };
    }

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
  private static async Task<Exception?> TryEndOwnedSessionAsync(
    long clearEpoch,
    CancellationToken cancellationToken)
  {
    try
    {
      await SessionFixture.EndRunLeaseAsync(clearEpoch, cancellationToken).ConfigureAwait(false);
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
  private static async Task EndOwnedSessionWithoutMaskingAsync(
    long clearEpoch,
    ITestResultSink sink,
    CancellationToken cancellationToken)
  {
    try
    {
      await SessionFixture.EndRunLeaseAsync(clearEpoch, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
#pragma warning disable CA1031 // Host boundary: do not replace an in-flight exception
    catch (Exception ex)
#pragma warning restore CA1031
    {
      string message = $"✗ Session fixture dispose failed: {ex.Message}";
      if (sink is TerminalSink terminalSink)
      {
        terminalSink.WriteDiagnostic(message);
      }
      else
      {
        Trace.TraceError(message);
      }
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
    string? message = null,
    TestNodeState state = TestNodeState.Failed,
    MethodInfo? method = null,
    bool lookupMethod = true,
    string? uid = null)
  {
    string nodeUid = uid ?? $"{testClass.FullName}.{hookName}";
    string nodeMessage = message ?? $"{exception.GetType().Name}: {exception.Message}";
    MethodInfo? hook = lookupMethod
      ? testClass.GetMethod(hookName, BindingFlags.Public | BindingFlags.Static)
      : method;
    DateTimeOffset stamp = DateTimeOffset.UtcNow;
    TestNodeInfo failedNode = new(
      nodeUid,
      hookName,
      state,
      TimeSpan.Zero,
      Exception: exception,
      Message: nodeMessage,
      Parameters: null,
      Method: hook,
      StartTime: stamp,
      EndTime: stamp
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
  /// Result of resolving a lifecycle hook by name.
  /// Method set and ValidationError null means no hook. ValidationError set means fail-fast.
  /// </summary>
  internal readonly record struct HookResolveResult(MethodInfo? Method, Exception? ValidationError);

  /// <summary>
  /// Resolves Setup, CleanUp, SetupOnce, or CleanUpOnce.
  /// Fail-fast on near-miss signatures. Prefers the most-derived declaring type.
  /// </summary>
  private static HookResolveResult ResolveHook(Type testClass, string name)
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
        return new HookResolveResult(
          Method: null,
          ValidationError: new InvalidOperationException(
            $"Type '{current.FullName}' has multiple methods named '{name}'. " +
            "Lifecycle hooks must have exactly one method."));
      }

      if (candidates.Count == 1)
      {
        MethodInfo candidate = candidates[0];
        if (!IsValidHookSignature(candidate))
        {
          return new HookResolveResult(
            Method: null,
            ValidationError: new InvalidOperationException(
              $"Type '{current.FullName}' method '{name}' must be 'public static Task {name}()' " +
              "(parameterless, non-generic). Non-conforming signatures fail the class."));
        }

        return new HookResolveResult(Method: candidate, ValidationError: null);
      }

      current = current.BaseType;
    }

    return new HookResolveResult(Method: null, ValidationError: null);
  }

  private static bool IsValidHookSignature(MethodInfo method)
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

    return await WaitForCompletionAsync(
      task,
      CurrentHookTimeoutMilliseconds,
      cancellationToken).ConfigureAwait(false);
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
      if (kind == WaitKind.TimedOut)
      {
        int milliseconds = CurrentHookTimeoutMilliseconds ?? 0;
        state.SetupOnceFailure = FailureMark.Mark(
          new TimeoutException(TimeoutAfter(milliseconds, "SetupOnce")),
          FailureMark.HookTimeout);
        return false;
      }

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
  /// Reports every selected case for a method with the hook outcome.
  /// Resolution uses <see cref="TestNodeState.Error"/>. A hook body uses
  /// <see cref="TestNodeState.Failed"/> or <see cref="TestNodeState.Timeout"/>.
  /// </summary>
  private static async Task ReportCasesFailedDueToHookAsync(
    IReadOnlyList<ReportedTestCase> cases,
    Exception hookException,
    TestNodeState state,
    ITestResultSink sink,
    List<TestNodeInfo> results)
  {
    string message = $"{hookException.GetType().Name}: {hookException.Message}";
    DateTimeOffset stamp = DateTimeOffset.UtcNow;
    foreach (ReportedTestCase testCase in cases)
    {
      TestNodeInfo failedNode = new(
        testCase.Uid,
        testCase.DisplayName,
        state,
        TimeSpan.Zero,
        Exception: hookException,
        Message: message,
        Parameters: testCase.Parameters,
        Method: testCase.Method,
        StartTime: stamp,
        EndTime: stamp);
      await sink.OnTestStartedAsync(failedNode).ConfigureAwait(false);
      await sink.OnTestCompletedAsync(failedNode).ConfigureAwait(false);
      results.Add(failedNode);
    }
  }
}
