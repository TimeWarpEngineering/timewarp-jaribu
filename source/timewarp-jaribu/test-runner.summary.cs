namespace TimeWarp.Jaribu;

using System.Globalization;
using TimeWarp.Terminal;

#region Purpose
// Terminal-sink entry points and the multi-class grand-total summary.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <param name="cancellationToken">
  /// Stops the run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed or the token fired.</returns>
  public static async Task<int> RunTests<T>(
    bool? clearCache = null,
    string? filterTag = null,
    CancellationToken cancellationToken = default) where T : class
  {
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;
    using TerminalSink sink = new();
    TestRunStats stats = await RunTestsAsync<T>(sink, filterTag, cancellationToken: cancellationToken);
    return ExitCodeFor(stats, cancellationToken);
  }

  /// <summary>
  /// Runs all registered test classes and returns an exit code.
  /// Uses TerminalSink internally for backward compatibility.
  /// </summary>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests.</param>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <param name="cancellationToken">
  /// Stops the run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed or the token fired.</returns>
  public static async Task<int> RunAllTests(
    bool? clearCache = null,
    string? filterTag = null,
    CancellationToken cancellationToken = default)
  {
    // clearCache retained for public API compatibility; runfile cache is managed externally.
    _ = clearCache;

    // Synthetic session so multi-class suites share session fixtures once.
    SessionFixture.BeginSession();
    bool disposeFailed = false;
    int exitCode = 1;
    try
    {
      exitCode = await RunAllTestsCore(filterTag, cancellationToken);
    }
    finally
    {
      try
      {
        await SessionFixture.EndSessionAsync(cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        // Cancellation already fails the run. Dispose may finish in the background.
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

    if (disposeFailed || cancellationToken.IsCancellationRequested)
    {
      return 1;
    }

    return exitCode;
  }

  /// <summary>
  /// Runs all registered test classes within the ambient session and returns the exit code.
  /// </summary>
  private static async Task<int> RunAllTestsCore(string? filterTag, CancellationToken cancellationToken)
  {
    // Snapshot so RegisterTests / ClearRegisteredTests during a class cannot tear the list.
    Type[] registeredClasses = [.. RegisteredTestClasses];
    if (registeredClasses.Length == 0)
    {
#pragma warning disable CA1849 // Terminal.WriteLine is acceptable in this warning context
      Terminal.WriteLine("⚠ No test classes registered. Use RegisterTests<T>() to register test classes.");
#pragma warning restore CA1849
      return cancellationToken.IsCancellationRequested ? 1 : 0;
    }

    List<TestRunStats> allStats = [];

    foreach (Type testClass in registeredClasses)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      TestRunStats stats = await RunRegisteredClassAsync(
        testClass,
        filterTag,
        cancellationToken).ConfigureAwait(false);
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

  /// <summary>
  /// Runs one registered class on a sink this method owns.
  /// The sink stays in a straight-line using so the run disposes it on every path.
  /// </summary>
  private static async Task<TestRunStats> RunRegisteredClassAsync(
    Type testClass,
    string? filterTag,
    CancellationToken cancellationToken)
  {
    using TerminalSink sink = new();
    return await RunTestsAsync(
      testClass,
      sink,
      filterTag,
      cancellationToken: cancellationToken).ConfigureAwait(false);
  }
}
