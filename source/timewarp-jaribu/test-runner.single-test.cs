namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Reflection;

#region Purpose
// Runs one test method: skip, per-test Setup, body, timeout, and CleanUp.
#endregion

public static partial class TestRunner
{
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
}
