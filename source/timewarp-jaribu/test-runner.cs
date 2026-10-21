namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TimeWarp.Terminal;

#region Purpose
// TestRunner - Main entry point for discovering and executing tests.
// Provides both sink-based async API for extensibility and backward-compatible
// synchronous-style methods that use TerminalSink internally.
// Supports per-test Setup/CleanUp, class-scoped SetupOnce/CleanUpOnce hooks,
// and session-scoped fixtures (RegisterSessionFixture + SessionFixture.GetAsync).
#endregion

#region Design
// The TestRunner uses a sink-based architecture where all test output flows through
// ITestResultSink implementations. This enables pluggable output destinations
// (terminal, MTP message bus, files, etc.) without changing the test execution logic.
//
// Execution flow:
//   RunTestsAsync(sink) -> sink.OnRunStartedAsync -> foreach test:
//     sink.OnTestStartedAsync -> RunSingleTestAsync -> sink.OnTestCompletedAsync
//   -> CleanUpOnce (if SetupOnce ran) -> sink.OnRunCompletedAsync -> return stats
//
// Class-scoped hooks (SetupOnce / CleanUpOnce):
//   Resolved once per class with fail-fast signature validation (public static Task,
//   parameterless). SetupOnce runs lazily before the first method that actually executes
//   (after method tag-filter and [Skip] short-circuits). CleanUpOnce runs in a finally
//   around the class loop only if SetupOnce was invoked. RunSingleTestAsync does not
//   participate in class hooks.
//
// [Input] identity:
//   Each row is its own node. TestCaseIdentity assigns {FullName}.{Method}[ordinal]
//   from metadata order. Discovery (DiscoverTestCases) and the run path share that
//   list. [Skip] and method-level tag misses stay one method-level node.
//   A bad once-hook signature fails that same reported list, so it does not expand
//   a skipped or tag-mismatched method. Direct RunSingleTestAsync calls pass the
//   same ordinal so the completed node id matches the in-progress node.
//
// Session-scoped fixtures (RegisterSessionFixture / SessionFixture.GetAsync):
//   Explicit registration (ModuleInitializer). Lazy create on first GetAsync within an
//   active session; disposed when the outermost session ends. Nesting:
//     MTP CreateTestSessionAsync → BeginTestSession; Close → EndTestSessionAsync
//     RunAllTests → begin/end wrap (inner RunTestsAsync does not dispose)
//     Lone RunTestsAsync → owns session-of-one if none active
//   Authors must NOT dispose session fixtures in CleanUpOnce.
//
// Per-test CleanUp runs only after the body outcome is stored. A throwing CleanUp
// cannot replace Failed, Error, or Timeout. The body exception stays on the node
// and the cleanup failure is appended to Message, labeled CleanUp. A passing body
// becomes Error and its message is prefixed with CleanUp.
//
// ownsSession teardown catches EndSessionAsync. The dispose failure is a synthetic
// failed node from the same helper as class-hook failures, with the message
// "Session fixture dispose failed: …". RunTests<T> maps Success to exit code 1.
// When the class run already threw, the dispose exception is logged and swallowed
// so it cannot replace the in-flight exception.
//
// DiscoverTests skips open generic method definitions.
// RunAllTestsCore enumerates a snapshot of RegisteredTestClasses.
// FailedCount includes Failed, Error, Timeout, and Cancelled.
//
// Backward compatibility:
//   RunTests<T>() creates a TerminalSink internally and delegates to RunTestsAsync<T>(sink).
//   This ensures existing test files continue to work unchanged.
#endregion

/// <summary>
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static class TestRunner
{
  /// <summary>
  /// Collection of registered test class types for batch execution.
  /// </summary>
  private static readonly List<Type> InternalRegisteredTestClasses = [];

  /// <summary>
  /// Gets the collection of registered test class types.
  /// </summary>
  public static IReadOnlyList<Type> RegisteredTestClasses => InternalRegisteredTestClasses;

  /// <summary>
  /// Registers a test class for batch execution with RunAllTests().
  /// </summary>
  /// <typeparam name="T">The test class to register.</typeparam>
  public static void RegisterTests<T>()
  {
    Type testType = typeof(T);
    if (!InternalRegisteredTestClasses.Contains(testType))
    {
      InternalRegisteredTestClasses.Add(testType);
    }
  }

  /// <summary>
  /// Clears all registered test classes.
  /// </summary>
  public static void ClearRegisteredTests()
  {
    InternalRegisteredTestClasses.Clear();
  }

  /// <summary>
  /// Registers a session-scoped fixture type. Call from a <c>[ModuleInitializer]</c>
  /// alongside <see cref="RegisterTests{T}"/>. Fail-fast on double-registration or when
  /// <typeparamref name="T"/> lacks <c>public static Task&lt;T&gt; CreateAsync()</c>.
  /// </summary>
  /// <typeparam name="T">
  /// Fixture type implementing <see cref="IAsyncDisposable"/> with
  /// <c>public static Task&lt;T&gt; CreateAsync()</c>.
  /// </typeparam>
  public static void RegisterSessionFixture<T>() where T : class, IAsyncDisposable
  {
    SessionFixture.Register(typeof(T));
  }

  /// <summary>
  /// Clears all session fixture registrations and resets session nesting to zero —
  /// a reset-the-world escape hatch for meta-tests only. Throws when a created
  /// fixture instance is still live; end the session first so it is disposed.
  /// Do not call from ordinary test code: resetting nesting silently unbalances
  /// any outer session (MTP or RunAllTests).
  /// </summary>
  /// <exception cref="InvalidOperationException">A created fixture instance is still live.</exception>
  public static void ClearRegisteredSessionFixtures()
  {
    SessionFixture.Clear();
  }

  /// <summary>
  /// Begins a host-owned test session (nesting-aware). Prefer for MTP adapters and
  /// multi-class drivers that share session fixtures; pair with
  /// <see cref="EndTestSessionAsync"/>.
  /// </summary>
  public static void BeginTestSession()
  {
    SessionFixture.BeginSession();
  }

  /// <summary>
  /// Ends a host-owned test session. When the outermost session ends, disposes all
  /// created session fixtures (registrations are kept).
  /// </summary>
  public static Task EndTestSessionAsync()
  {
    return SessionFixture.EndSessionAsync();
  }

  /// <summary>
  /// Discovers all test methods in the specified test class.
  /// Test methods are public static methods that return Task, excluding lifecycle hooks
  /// (Setup, CleanUp, SetupOnce, CleanUpOnce) and open generic method definitions.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <returns>Enumerable of test method infos.</returns>
  public static IEnumerable<MethodInfo> DiscoverTests(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    return testClass
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method =>
        method.IsPublic &&
        method.IsStatic &&
        method.ReturnType == typeof(Task) &&
        !method.IsGenericMethodDefinition &&
        method.Name is not "CleanUp" and not "Setup" and not "SetupOnce" and not "CleanUpOnce");
  }

  /// <summary>
  /// Lists every reported node for the class, expanding <see cref="InputAttribute"/> rows
  /// the same way a normal run does.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <param name="filterTag">
  /// Optional tag filter. A method whose own tags exist and none match contributes one node.
  /// </param>
  /// <returns>Reported cases in discovery order.</returns>
  public static IEnumerable<ReportedTestCase> DiscoverTestCases(Type testClass, string? filterTag = null)
  {
    return TestCaseIdentity.DiscoverTestCases(testClass, filterTag);
  }

  /// <summary>
  /// Runs a single test method and returns TestNodeInfo.
  /// Handles per-test Setup, test execution, CleanUp, timeout, skip, and exceptions.
  /// Does not invoke class-scoped SetupOnce/CleanUpOnce; those run only through
  /// RunTests/RunAllTests/RunTestsAsync. Callers driving single tests own fixture lifetime.
  /// </summary>
  /// <param name="testClass">The test class containing the method.</param>
  /// <param name="method">The test method to run.</param>
  /// <param name="parameters">Optional parameters for parameterized tests.</param>
  /// <param name="inputOrdinal">
  /// Metadata ordinal of the <see cref="InputAttribute"/> row. When set, the node id matches discovery.
  /// </param>
  /// <returns>A TestNodeInfo with the test outcome.</returns>
  public static async Task<TestNodeInfo> RunSingleTestAsync(
    Type testClass,
    MethodInfo method,
    object?[]? parameters = null,
    int? inputOrdinal = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    parameters ??= [];

    string testNodeUid = TestCaseIdentity.CaseUid(testClass, method, inputOrdinal, parameters);
    string displayName = TestCaseIdentity.FormatDisplayName(method.Name, parameters);

    Stopwatch stopwatch = Stopwatch.StartNew();

    TestNodeInfo Complete(
      TestNodeState state,
      TimeSpan? duration,
      Exception? exception,
      string? message)
    {
      return new TestNodeInfo(
        testNodeUid,
        displayName,
        state,
        duration,
        exception,
        message,
        parameters.Length > 0 ? parameters.ToList() : null,
        method);
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      stopwatch.Stop();
      return Complete(TestNodeState.Skipped, stopwatch.Elapsed, exception: null, skipAttr.Reason);
    }

    try
    {
      // Run Setup
      await InvokeSetupForType(testClass);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      return Complete(
        TestNodeState.Failed,
        stopwatch.Elapsed,
        ex.InnerException,
        $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      return Complete(
        TestNodeState.Error,
        stopwatch.Elapsed,
        ex,
        $"{ex.GetType().Name}: {ex.Message}");
    }

    TestNodeInfo outcome;
    try
    {
      Task? testTask = method.Invoke(null, parameters) as Task;
      if (testTask is not null)
      {
        TimeoutAttribute? timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
        if (timeoutAttr is not null)
        {
          Task timeoutTask = Task.Delay(timeoutAttr.Milliseconds);
          Task completedTask = await Task.WhenAny(testTask, timeoutTask);
          if (completedTask == timeoutTask)
          {
            stopwatch.Stop();
            outcome = Complete(
              TestNodeState.Timeout,
              stopwatch.Elapsed,
              new TimeoutException($"Test exceeded timeout of {timeoutAttr.Milliseconds}ms"),
              $"Timeout after {timeoutAttr.Milliseconds}ms");
          }
          else
          {
            await testTask;
            stopwatch.Stop();
            outcome = Complete(TestNodeState.Passed, stopwatch.Elapsed, exception: null, message: null);
          }
        }
        else
        {
          await testTask;
          stopwatch.Stop();
          outcome = Complete(TestNodeState.Passed, stopwatch.Elapsed, exception: null, message: null);
        }
      }
      else
      {
        stopwatch.Stop();
        outcome = Complete(TestNodeState.Passed, stopwatch.Elapsed, exception: null, message: null);
      }
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      stopwatch.Stop();
      outcome = Complete(
        TestNodeState.Failed,
        stopwatch.Elapsed,
        ex.InnerException,
        $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      outcome = Complete(
        TestNodeState.Error,
        stopwatch.Elapsed,
        ex,
        $"{ex.GetType().Name}: {ex.Message}");
    }

    Exception? cleanupException = await InvokeCleanupIsolatedAsync(testClass);
    if (cleanupException is null)
    {
      return outcome;
    }

    return FoldCleanupFailure(outcome, cleanupException);
  }

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

  /// <summary>
  /// Keeps the cases the predicate accepts. A null predicate keeps every case.
  /// </summary>
  private static List<ReportedTestCase> SelectCases(
    IReadOnlyList<ReportedTestCase> cases,
    Func<ReportedTestCase, bool>? casePredicate)
  {
    if (casePredicate is null)
    {
      return [.. cases];
    }

    return [.. cases.Where(casePredicate)];
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests (Skipped semantics).</param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync<T>(
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null) where T : class
  {
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(typeof(T), sink, filterTag, methodNameContains, methodPredicate, casePredicate);
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <param name="testClass">The test class type.</param>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">Optional tag to filter tests (Skipped semantics).</param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(testClass, sink, filterTag, methodNameContains, methodPredicate, casePredicate);
  }

  /// <summary>
  /// Core implementation for running tests with a sink.
  /// Owns a session-of-one when no outer session is active.
  /// </summary>
  private static async Task<TestRunStats> RunTestsAsyncCore(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    bool ownsSession = false;
    if (!SessionFixture.IsSessionActive)
    {
      SessionFixture.BeginSession();
      ownsSession = true;
    }

    try
    {
      DateTimeOffset startTime = DateTimeOffset.Now;
      Stopwatch overallStopwatch = Stopwatch.StartNew();
      List<TestNodeInfo> results = [];

      async Task EndOwnedSessionIntoResultsAsync()
      {
        if (!ownsSession)
        {
          return;
        }

        Exception? disposeFailure = await TryEndOwnedSessionAsync().ConfigureAwait(false);
        ownsSession = false;
        if (disposeFailure is not null)
        {
          await EmitSyntheticHookFailureAsync
          (
            testClass,
            "SessionFixtureDispose",
            disposeFailure,
            sink,
            results,
            $"Session fixture dispose failed: {disposeFailure.Message}"
          ).ConfigureAwait(false);
        }
      }

      // Check environment variable if filterTag not explicitly provided
      filterTag ??= Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");

      string className = testClass.Name.Replace("Tests", "", StringComparison.Ordinal);

      // Check if test class matches filter tag (if specified)
      if (filterTag is not null)
      {
        TestTagAttribute[] classTags = testClass.GetCustomAttributes<TestTagAttribute>().ToArray();
        if (classTags.Length > 0 && !classTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
        {
          // Class has tags but none match the filter - skip entire class
          await sink.OnRunStartedAsync(className, filterTag);
          await EndOwnedSessionIntoResultsAsync().ConfigureAwait(false);
          overallStopwatch.Stop();
          int filteredFailures = results.Count(r => TestRunStats.IsFailingState(r.State));
          TestRunStats emptyStats = new(
            className,
            startTime,
            overallStopwatch.Elapsed,
            PassedCount: 0,
            FailedCount: filteredFailures,
            SkippedCount: 0
          );
          await sink.OnRunCompletedAsync(emptyStats, results);
          return emptyStats;
        }
      }

      // Notify sink that run is starting
      await sink.OnRunStartedAsync(className, filterTag);

      List<MethodInfo> testMethods = DiscoverTests(testClass).ToList();

      // Selection filters: omit methods (no Skipped nodes) before execution.
      if (methodNameContains is not null)
      {
        testMethods = [.. testMethods.Where(m =>
          m.Name.Contains(methodNameContains, StringComparison.OrdinalIgnoreCase))];
      }

      if (methodPredicate is not null)
      {
        testMethods = [.. testMethods.Where(methodPredicate)];
      }

      OnceHookResolveResult setupOnceResolve = ResolveOnceHook(testClass, "SetupOnce");
      OnceHookResolveResult cleanUpOnceResolve = ResolveOnceHook(testClass, "CleanUpOnce");
      Exception? resolutionError = setupOnceResolve.ValidationError ?? cleanUpOnceResolve.ValidationError;

      if (resolutionError is not null)
      {
        // Bad once-hook signature: fail the nodes discovery lists, without invoking CleanUpOnce.
        foreach (MethodInfo method in testMethods)
        {
          List<ReportedTestCase> cases = SelectCases(
            TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag),
            casePredicate);
          await ReportCasesFailedDueToHookAsync(cases, resolutionError, sink, results);
        }
      }
      else
      {
        ClassOnceState onceState = new()
        {
          SetupOnce = setupOnceResolve.Method,
          CleanUpOnce = cleanUpOnceResolve.Method
        };

        try
        {
          foreach (MethodInfo method in testMethods)
          {
            List<TestNodeInfo> methodResults = await RunTestWithSinkAsync(
              testClass,
              method,
              sink,
              filterTag,
              onceState,
              casePredicate);
            results.AddRange(methodResults);
          }
        }
        finally
        {
          if (onceState.SetupOnceInvoked && onceState.CleanUpOnce is not null)
          {
            try
            {
              await InvokeOnceHookAsync(onceState.CleanUpOnce);
            }
            catch (Exception cleanUpEx)
            {
              await EmitSyntheticHookFailureAsync(testClass, "CleanUpOnce", cleanUpEx, sink, results);
            }
          }
        }
      }

      await EndOwnedSessionIntoResultsAsync().ConfigureAwait(false);

      overallStopwatch.Stop();

      // Calculate stats
      int passedCount = results.Count(r => r.State == TestNodeState.Passed);
      int failedCount = results.Count(r => TestRunStats.IsFailingState(r.State));
      int skippedCount = results.Count(r => r.State == TestNodeState.Skipped);

      TestRunStats stats = new(
        className,
        startTime,
        overallStopwatch.Elapsed,
        passedCount,
        failedCount,
        skippedCount
      );

      // Notify sink that run is completed
      await sink.OnRunCompletedAsync(stats, results);

      return stats;
    }
    finally
    {
      if (ownsSession)
      {
        await EndOwnedSessionWithoutMaskingAsync().ConfigureAwait(false);
      }
    }
  }

  /// <summary>
  /// Runs a single test method with sink notifications.
  /// Lazy SetupOnce runs after tag-skip and [Skip] short-circuits, before body/[Input].
  /// </summary>
  private static async Task<List<TestNodeInfo>> RunTestWithSinkAsync(
    Type testClass,
    MethodInfo method,
    ITestResultSink sink,
    string? filterTag,
    ClassOnceState onceState,
    Func<ReportedTestCase, bool>? casePredicate)
  {
    List<TestNodeInfo> results = [];

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Method has tags but none match - report as skipped (one method-level node).
        ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
        if (casePredicate is not null && !casePredicate(skipCase))
        {
          return results;
        }

        TestNodeInfo skipNode = new(
          skipCase.Uid,
          skipCase.DisplayName,
          TestNodeState.Skipped,
          TimeSpan.Zero,
          Exception: null,
          Message: $"No matching tag '{filterTag}'",
          Parameters: null,
          Method: method
        );
        await sink.OnTestStartedAsync(skipNode);
        await sink.OnTestCompletedAsync(skipNode);
        results.Add(skipNode);
        return results;
      }
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      ReportedTestCase skipCase = TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)[0];
      if (casePredicate is not null && !casePredicate(skipCase))
      {
        return results;
      }

      TestNodeInfo skipNode = new(
        skipCase.Uid,
        skipCase.DisplayName,
        TestNodeState.Skipped,
        TimeSpan.Zero,
        Exception: null,
        Message: skipAttr.Reason,
        Parameters: null,
        Method: method
      );
      await sink.OnTestStartedAsync(skipNode);
      await sink.OnTestCompletedAsync(skipNode);
      results.Add(skipNode);
      return results;
    }

    List<ReportedTestCase> cases = SelectCases(
      TestCaseIdentity.ExpandInputs(testClass, method),
      casePredicate);
    if (cases.Count == 0)
    {
      return results;
    }

    // Lazy class-scoped SetupOnce immediately before first real execution path
    await EnsureSetupOnceAsync(onceState);
    if (onceState.SetupOnceFailure is not null)
    {
      await ReportCasesFailedDueToHookAsync(cases, onceState.SetupOnceFailure, sink, results);
      return results;
    }

    foreach (ReportedTestCase testCase in cases)
    {
      TestNodeInfo inProgressNode = new(
        testCase.Uid,
        testCase.DisplayName,
        TestNodeState.InProgress,
        Parameters: testCase.Parameters,
        Method: method);

      await sink.OnTestStartedAsync(inProgressNode);

      TestNodeInfo result = await RunSingleTestAsync(
        testClass,
        method,
        testCase.ToParameterArray(),
        testCase.InputOrdinal);

      await sink.OnTestCompletedAsync(result);
      results.Add(result);
    }

    return results;
  }

  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunTests<T>(bool? clearCache = null, string? filterTag = null) where T : class
  {
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;
    using TerminalSink sink = new();
    TestRunStats stats = await RunTestsAsync<T>(sink, filterTag);
    return stats.Success ? 0 : 1;
  }

  /// <summary>
  /// Runs all registered test classes and returns an exit code.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunAllTests(bool? clearCache = null, string? filterTag = null)
  {
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;

    // Synthetic session so multi-class suites share session fixtures once.
    SessionFixture.BeginSession();
    bool disposeFailed = false;
    int exitCode = 1;
    try
    {
      exitCode = await RunAllTestsCore(filterTag);
    }
    finally
    {
      try
      {
        await SessionFixture.EndSessionAsync().ConfigureAwait(false);
      }
#pragma warning disable CA1031 // Host boundary: dispose failure must fail the run, not crash it
      catch (Exception ex)
#pragma warning restore CA1031
      {
        // On a body exception this logs instead of masking it; on the normal
        // path it folds into a failing exit code.
        disposeFailed = true;
#pragma warning disable CA1849
        Terminal.WriteLine($"✗ Session fixture dispose failed: {ex.Message}");
#pragma warning restore CA1849
      }
    }

    return disposeFailed ? 1 : exitCode;
  }

  /// <summary>
  /// Runs all registered test classes within the ambient session and returns the exit code.
  /// </summary>
  private static async Task<int> RunAllTestsCore(string? filterTag)
  {
    // Snapshot so RegisterTests / ClearRegisteredTests during a class cannot tear the list.
    Type[] registeredClasses = [.. RegisteredTestClasses];
    if (registeredClasses.Length == 0)
    {
#pragma warning disable CA1849 // Terminal.WriteLine is acceptable in this warning context
      Terminal.WriteLine("⚠ No test classes registered. Use RegisterTests<T>() to register test classes.");
#pragma warning restore CA1849
      return 0;
    }

    List<TestRunStats> allStats = [];

    foreach (Type testClass in registeredClasses)
    {
      using TerminalSink sink = new();
      TestRunStats stats = await RunTestsAsync(testClass, sink, filterTag);
      allStats.Add(stats);
    }

    // Print grand total summary when multiple classes were run
    if (allStats.Count > 1)
    {
      int totalPassed = allStats.Sum(s => s.PassedCount);
      int totalFailed = allStats.Sum(s => s.FailedCount);
      int totalSkipped = allStats.Sum(s => s.SkippedCount);
      int totalTests = totalPassed + totalFailed + totalSkipped;

#pragma warning disable CA1849
      TimeWarpTerminal terminal = TimeWarpTerminal.Default;

      terminal
        .WriteRule(rule => rule.Title("Grand Total").Style(LineStyle.Doubled))
        .WriteTable(table =>
        {
          table
            .AddColumn("Class")
            .AddColumn("Passed", Alignment.Right)
            .AddColumn("Failed", Alignment.Right)
            .AddColumn("Skipped", Alignment.Right)
            .AddColumn("Total", Alignment.Right)
            .Border(BorderStyle.Rounded);

          foreach (TestRunStats classStats in allStats)
          {
            table.AddRow
            (
              classStats.ClassName,
              classStats.PassedCount.ToString(CultureInfo.InvariantCulture).Green(),
              classStats.FailedCount > 0
                ? classStats.FailedCount.ToString(CultureInfo.InvariantCulture).Red()
                : classStats.FailedCount.ToString(CultureInfo.InvariantCulture),
              classStats.SkippedCount > 0
                ? classStats.SkippedCount.ToString(CultureInfo.InvariantCulture).Yellow()
                : classStats.SkippedCount.ToString(CultureInfo.InvariantCulture),
              classStats.TotalTests.ToString(CultureInfo.InvariantCulture)
            );
          }
        })
        .WriteLine(string.Empty)
        .WriteLine($"{"Total:".Bold()} {totalTests.ToString(CultureInfo.InvariantCulture)}")
        .WriteLine($"{"Passed:".Green()} {totalPassed.ToString(CultureInfo.InvariantCulture)}");

      if (totalFailed > 0)
      {
        terminal.WriteLine($"{"Failed:".Red()} {totalFailed.ToString(CultureInfo.InvariantCulture)}");
      }

      if (totalSkipped > 0)
      {
        terminal.WriteLine($"{"Skipped:".Yellow()} {totalSkipped.ToString(CultureInfo.InvariantCulture)}");
      }
#pragma warning restore CA1849
    }

    return allStats.Any(s => !s.Success) ? 1 : 0;
  }
}
