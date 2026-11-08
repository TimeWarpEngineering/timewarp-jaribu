namespace TimeWarp.Jaribu;

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
//   A bad Setup, CleanUp, SetupOnce, or CleanUpOnce signature fails that same
//   reported list, so it does not expand a skipped or tag-mismatched method.
//   The message names the type that declares the method. Direct RunSingleTestAsync
//   calls pass the
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
// Outcomes:
//   Exceptions thrown by the test or by Setup, sync or async, are Failed.
//   Error is hook resolution, session fixture creation, reflection invoke
//   failures, and a throwing CleanUp after a passing body.
//
// Timeouts:
//   A method TimeoutAttribute limits that body. A class TimeoutAttribute, or
//   DefaultHookTimeout when the class has none, limits Setup, CleanUp,
//   SetupOnce, CleanUpOnce, and session fixture creation.
//   The wait uses CancellationTokenSource and TimeProvider. The timer is
//   disposed when the wait ends. An abandoned task is observed. A later fault
//   is traced. A fault already stored on the task is reported instead of Timeout.
//
// Per-test CleanUp runs only after the body outcome is stored. A throwing CleanUp
// cannot replace Failed, Error, or Timeout. The body exception stays on the node
// and the cleanup failure is appended to Message, labeled CleanUp. A passing body
// becomes Error, or Timeout when CleanUp itself times out. The message is prefixed
// with CleanUp.
//
// ownsSession teardown catches EndSessionAsync. The dispose failure is a synthetic
// failed node from the same helper as class-hook failures, with the message
// "Session fixture dispose failed: …". RunTests<T> maps Success to exit code 1.
// When the class run already threw, the dispose exception is logged and swallowed
// so it cannot replace the in-flight exception.
//
// DiscoverTests skips open generic method definitions. Those methods, and
// public static methods that return Task<T>, ValueTask, ValueTask<T>, or
// async void, are near-miss tests: a warning by default, an Error when
// strictSignatures is set. A class with no discoverable tests fails unless
// allowEmpty is set. A tag or method filter that selects nothing does not.
// RegisteredTestClasses returns a copy under RegistrationGate.
// RunAllTestsCore and JaribuTestFramework.ExecuteRequestAsync each enumerate
// that snapshot.
// FailedCount includes Failed, Error, Timeout, and Cancelled.
//
// Cancellation:
//   RunTests, RunAllTests, and RunTestsAsync take a CancellationToken
//   (default None). The token stops waiting for the in-flight test, Setup,
//   CleanUp, once-hooks, and session-fixture create or dispose. The interrupted
//   node is Cancelled. Later tests are not started. Timeout stays Timeout
//   unless the token has also fired. A cancelled run exits non-zero.
//
// Backward compatibility:
//   RunTests<T>() creates a TerminalSink internally and delegates to RunTestsAsync<T>(sink).
//   This ensures existing test files continue to work unchanged.
//
// Partial files hold registration, discovery, single-test execution, hooks,
// class run, and summary. The split is file organization; the public API is one type.
#endregion

/// <summary>
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static partial class TestRunner
{
  /// <summary>
  /// Collection of registered test class types for batch execution.
  /// </summary>
  private static readonly List<Type> InternalRegisteredTestClasses = [];

  /// <summary>
  /// Guards <see cref="InternalRegisteredTestClasses"/>. The public list is a copy.
  /// </summary>
  private static readonly object RegistrationGate = new();

  /// <summary>
  /// Gets a snapshot of the registered test class types.
  /// Later <see cref="RegisterTests{T}"/> or <see cref="ClearRegisteredTests"/> calls
  /// do not change a list the caller already holds.
  /// </summary>
  public static IReadOnlyList<Type> RegisteredTestClasses
  {
    get
    {
      lock (RegistrationGate)
      {
        return [.. InternalRegisteredTestClasses];
      }
    }
  }
}
