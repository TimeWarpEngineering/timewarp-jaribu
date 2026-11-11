namespace TimeWarp.Jaribu;

#region Purpose
// Counts and timing for one class run.
#endregion

/// <summary>
/// Contains aggregated statistics from running all tests in a test class.
/// </summary>
/// <param name="ClassName">The name of the test class.</param>
/// <param name="StartTime">When the test run started.</param>
/// <param name="Duration">Total time for all tests.</param>
/// <param name="PassedCount">Number of tests that passed.</param>
/// <param name="FailedCount">
/// Number of tests in a failing state: Failed, Error, Timeout, or Cancelled.
/// </param>
/// <param name="SkippedCount">Number of tests that were skipped.</param>
public record TestRunStats
(
  string ClassName,
  DateTimeOffset StartTime,
  TimeSpan Duration,
  int PassedCount,
  int FailedCount,
  int SkippedCount
)
{
  /// <summary>
  /// Total number of tests executed.
  /// </summary>
  public int TotalTests => PassedCount + FailedCount + SkippedCount;

  /// <summary>
  /// Whether every counted test passed or was skipped.
  /// A non-zero <see cref="FailedCount"/> makes this false, including Cancelled.
  /// Signature warnings alone do not.
  /// </summary>
  public bool Success => FailedCount == 0;

  /// <summary>
  /// Near-miss public static test methods that were not executed.
  /// Empty when the class has none. <c>strictSignatures</c> also reports each as an Error node,
  /// except when a method predicate or case predicate selected no nodes in the class.
  /// </summary>
  public IReadOnlyList<string> SignatureWarnings { get; init; } = [];

  /// <summary>
  /// True when <paramref name="state"/> counts toward <see cref="FailedCount"/>.
  /// Failed, Error, Timeout, and Cancelled all fail the run. Skipped does not.
  /// MTP 2.3.3 counts CancelledTestNodeStateProperty toward the failed-test exit code.
  /// </summary>
  /// <param name="state">The node state to classify.</param>
  /// <returns>True when the state fails the run.</returns>
  public static bool IsFailingState(TestNodeState state)
  {
    return state is TestNodeState.Failed
      or TestNodeState.Error
      or TestNodeState.Timeout
      or TestNodeState.Cancelled;
  }
}
