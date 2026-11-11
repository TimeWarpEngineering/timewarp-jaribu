namespace TimeWarp.Jaribu;

#region Purpose
// Sink that discards every event. Tests and benchmarks use it when output is not under test.
#endregion

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

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestDiscoveredAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestStartedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="className"/> is null.</exception>
  public Task OnRunStartedAsync(string className, string? filterTag = null)
  {
    ArgumentNullException.ThrowIfNull(className);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException">
  /// <paramref name="stats"/> or <paramref name="results"/> is null.
  /// </exception>
  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results)
  {
    ArgumentNullException.ThrowIfNull(stats);
    ArgumentNullException.ThrowIfNull(results);
    return Task.CompletedTask;
  }
}
