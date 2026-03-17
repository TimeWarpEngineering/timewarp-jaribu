namespace TimeWarp.Jaribu;

/// <summary>
/// A silent test result sink that discards all events. Useful for testing and benchmarking.
/// </summary>
public sealed class NullSink : ITestResultSink
{
  /// <summary>
  /// Singleton instance.
  /// </summary>
  public static readonly NullSink Instance = new();

  private NullSink() { }

  public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
  public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;
  public Task OnTestCompletedAsync(TestNodeInfo node) => Task.CompletedTask;
  public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;
  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
}
