namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Runtime.ExceptionServices;
using TimeWarp.Terminal;

#region Purpose
// Resolves and invokes per-test Setup/CleanUp and class-scoped SetupOnce/CleanUpOnce.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Invokes the Setup method for a given type if it exists.
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
  /// </summary>
  private static async Task InvokeSetupForType(Type testClass)
  {
    MethodInfo? setupMethod = testClass.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
    if (setupMethod is not null && setupMethod.ReturnType == typeof(Task))
    {
      if (setupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Invokes the CleanUp method for a given type if it exists.
  /// Non-conforming signatures are silently ignored (legacy per-test behavior).
  /// </summary>
  private static async Task InvokeCleanupForType(Type testClass)
  {
    MethodInfo? cleanupMethod = testClass.GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod is not null && cleanupMethod.ReturnType == typeof(Task))
    {
      if (cleanupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Runs CleanUp without letting its exception escape. Returns null when cleanup succeeds
  /// or no conforming CleanUp exists.
  /// </summary>
  private static async Task<Exception?> InvokeCleanupIsolatedAsync(Type testClass)
  {
    try
    {
      await InvokeCleanupForType(testClass);
      return null;
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      return ex.InnerException;
    }
#pragma warning disable CA1031 // Cleanup failure is data for the node, not a crash
    catch (Exception ex)
#pragma warning restore CA1031
    {
      return ex;
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
  private static async Task<Exception?> TryEndOwnedSessionAsync()
  {
    try
    {
      await SessionFixture.EndSessionAsync().ConfigureAwait(false);
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
  /// </summary>
  private static async Task EndOwnedSessionWithoutMaskingAsync()
  {
    try
    {
      await SessionFixture.EndSessionAsync().ConfigureAwait(false);
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
  /// </summary>
  private static async Task InvokeOnceHookAsync(MethodInfo method)
  {
    try
    {
      object? result = method.Invoke(null, null);
      if (result is Task task)
      {
        await task.ConfigureAwait(false);
      }
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
      throw; // unreachable; satisfies definite-assignment analysis
    }
  }

  /// <summary>
  /// Idempotent: invokes SetupOnce on first call. Sets SetupOnceInvoked before await
  /// so a throwing SetupOnce still pairs with CleanUpOnce.
  /// </summary>
  private static async Task EnsureSetupOnceAsync(ClassOnceState state)
  {
    if (state.SetupOnceInvoked || state.SetupOnce is null)
    {
      return;
    }

    state.SetupOnceInvoked = true;
    try
    {
      await InvokeOnceHookAsync(state.SetupOnce).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      state.SetupOnceFailure = ex;
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
