namespace TimeWarp.Jaribu;

/// <summary>
/// Contains aggregated statistics from running all tests in a test class.
/// </summary>
/// <param name="ClassName">The name of the test class.</param>
/// <param name="StartTime">When the test run started.</param>
/// <param name="Duration">Total time for all tests.</param>
/// <param name="PassedCount">Number of tests that passed.</param>
/// <param name="FailedCount">Number of tests that failed.</param>
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
  /// Whether all tests passed (or were skipped).
  /// </summary>
  public bool Success => FailedCount == 0;
}
