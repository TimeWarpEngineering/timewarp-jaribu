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
  /// <param name="node">The discovered node.</param>
  Task OnTestDiscoveredAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test node begins execution.
  /// </summary>
  /// <param name="node">The node that is starting.</param>
  Task OnTestStartedAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test node finishes execution with a terminal state.
  /// </summary>
  /// <param name="node">The node that finished.</param>
  Task OnTestCompletedAsync(TestNodeInfo node);

  /// <summary>
  /// Called when a test run begins for a class.
  /// </summary>
  /// <param name="className">Reported class name for this run.</param>
  /// <param name="filterTag">Active tag filter, or null when the run is unfiltered.</param>
  Task OnRunStartedAsync(string className, string? filterTag = null);

  /// <summary>
  /// Called when a test run completes for a class with aggregated stats.
  /// </summary>
  /// <param name="stats">Aggregated counts for the class.</param>
  /// <param name="results">Nodes the run reported, in report order.</param>
  Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results);
}
