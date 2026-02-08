namespace TimeWarp.Jaribu;

/// <summary>
/// Abstraction for receiving test execution events. Enables pluggable output destinations
/// (terminal, MTP message bus, files, etc.) without changing the TestRunner.
/// </summary>
public interface ITestResultSink
{
  /// <summary>
  /// Called when a test node has been discovered but not yet executed.
  /// </summary>
  Task OnTestDiscoveredAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test node begins execution.
  /// </summary>
  Task OnTestStartedAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test node finishes execution with a terminal state.
  /// </summary>
  Task OnTestCompletedAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test run begins for a class.
  /// </summary>
  Task OnRunStartedAsync(string className, string? filterTag = null);

  /// <summary>
  /// Called when a test run completes for a class with aggregated stats.
  /// </summary>
  Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results);
}
