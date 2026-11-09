namespace TimeWarp.Jaribu;

using System.Globalization;
using TimeWarp.Terminal;

#region Purpose
// Terminal-sink entry points and the multi-class grand-total summary.
#endregion

#region Design
// RunAllTests owns one terminal for the call unless the caller passes one.
// Class sinks, the empty-registration line, session-dispose failures, and the
// grand total all write there. A caller-owned terminal is not disposed.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// Uses TerminalSink internally for backward compatibility.
  /// The .NET runfile cache is left untouched.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature fails the run. The default warns.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a class with no discoverable tests succeeds. The default fails that run.
  /// </param>
  /// <param name="terminal">
  /// Terminal that receives this class's output. A null value creates one for the call.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// Named use keeps a new optional parameter from shifting this token.
  /// </param>
  /// <returns>
  /// Exit code: 0 when the run succeeded, 1 when a test failed, no tests executed, or the token fired.
  /// </returns>
  public static async Task<int> RunTests<T>(
    string? filterTag = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    ITerminal? terminal = null,
    CancellationToken cancellationToken = default) where T : class
  {
    using TerminalSink sink = terminal is null ? new TerminalSink() : new TerminalSink(terminal);
    TestRunStats stats = await RunTestsAsync<T>(
      sink,
      filterTag,
      cancellationToken: cancellationToken,
      strictSignatures: strictSignatures,
      allowEmpty: allowEmpty);
    return ExitCodeFor(stats, cancellationToken);
  }

  /// <summary>
  /// Runs all registered test classes and returns an exit code.
  /// Uses TerminalSink internally for backward compatibility.
  /// The .NET runfile cache is left untouched.
  /// </summary>
  /// <param name="filterTag">Optional tag to filter tests.</param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature fails the run. The default warns.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a run with no registered classes or no discoverable tests succeeds.
  /// The default fails that run.
  /// </param>
  /// <param name="terminal">
  /// Terminal for every class, the empty-registration line, session-dispose failures, and the grand total.
  /// A null value creates one terminal for the call and disposes it.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// Named use keeps a new optional parameter from shifting this token.
  /// </param>
  /// <returns>
  /// Exit code: 0 when the run succeeded, 1 when a test failed, no tests executed, or the token fired.
  /// </returns>
  public static async Task<int> RunAllTests(
    string? filterTag = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    ITerminal? terminal = null,
    CancellationToken cancellationToken = default)
  {
    bool ownsTerminal = terminal is null;
    ITerminal output = terminal ?? new TimeWarpTerminal();
    try
    {
      // Synthetic session so multi-class suites share session fixtures once.
      SessionFixture.BeginSession();
      bool disposeFailed = false;
      int exitCode = 1;
      try
      {
        exitCode = await RunAllTestsCore(
          filterTag,
          strictSignatures,
          allowEmpty,
          output,
          cancellationToken);
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
          output.WriteLine($"✗ Session fixture dispose failed: {ex.Message}");
#pragma warning restore CA1849
        }
      }

      if (disposeFailed || cancellationToken.IsCancellationRequested)
      {
        return 1;
      }

      return exitCode;
    }
    finally
    {
      if (ownsTerminal && output is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>
  /// Runs all registered test classes within the ambient session and returns the exit code.
  /// </summary>
  private static async Task<int> RunAllTestsCore(
    string? filterTag,
    bool strictSignatures,
    bool allowEmpty,
    ITerminal terminal,
    CancellationToken cancellationToken)
  {
    // Snapshot so RegisterTests / ClearRegisteredTests during a class cannot tear the list.
    Type[] registeredClasses = [.. RegisteredTestClasses];
    if (registeredClasses.Length == 0)
    {
#pragma warning disable CA1849 // Terminal.WriteLine is acceptable in this warning context
      terminal.WriteLine(
        "⚠ No test classes registered. Use RegisterTests<T>() to register test classes. " +
        "An empty run fails unless allowEmpty is true.");
#pragma warning restore CA1849
      if (allowEmpty && !cancellationToken.IsCancellationRequested)
      {
        return 0;
      }

      return 1;
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
        strictSignatures,
        allowEmpty,
        terminal,
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
    bool strictSignatures,
    bool allowEmpty,
    ITerminal terminal,
    CancellationToken cancellationToken)
  {
    using TerminalSink sink = new(terminal);
    return await RunTestsAsync(
      testClass,
      sink,
      filterTag,
      cancellationToken: cancellationToken,
      strictSignatures: strictSignatures,
      allowEmpty: allowEmpty).ConfigureAwait(false);
  }
}
