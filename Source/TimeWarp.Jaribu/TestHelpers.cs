namespace TimeWarp.Jaribu;

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TimeWarp.Terminal;

/// <summary>
/// Helper utilities for test formatting and common patterns.
/// </summary>
public static partial class TestHelpers
{
  /// <summary>
  /// Converts PascalCase test method names to readable format.
  /// Example: "CatchAllInOptionShouldFail" → "Catch All In Option Should Fail"
  /// </summary>
  public static string FormatTestName(string name) =>
    PascalCaseRegex().Replace(name, " $1").Trim();

  /// <summary>
  /// Logs a test pass with formatted output.
  /// </summary>
  /// <summary>
  /// Logs a test pass status.
  /// </summary>
  public static void TestPassed() =>
    Console.WriteLine("  ✓ PASSED");

  /// <summary>
  /// Logs a test failure status with reason.
  /// </summary>
  public static void TestFailed(string reason) =>
    Console.WriteLine($"  ✗ FAILED: {reason}");

  /// <summary>
  /// Logs a test skipped status with reason.
  /// </summary>
  public static void TestSkipped(string reason) =>
    Console.WriteLine($"  ⚠ SKIPPED: {reason}");

  /// <summary>
  /// Prints test results in a formatted table with colored status indicators.
  /// </summary>
  /// <param name="summary">The test run summary containing all results.</param>
  /// <param name="terminal">Optional terminal for output. Uses NuruTerminal if not specified.</param>
  /// <param name="maxMessageWidth">Maximum width for message column before truncation. Default 50.</param>
  public static void PrintResultsTable(TestRunSummary summary, ITerminal? terminal = null, int maxMessageWidth = 50)
  {
    ArgumentNullException.ThrowIfNull(summary);
    terminal ??= new TimeWarpTerminal();

    terminal.WriteTable(table =>
    {
      table
        .AddColumn("Test")
        .AddColumn("Status")
        .AddColumn("Duration", Alignment.Right)
        .AddColumn("Message")
        .Border(BorderStyle.Rounded);

      foreach (TestResult result in summary.Results)
      {
        string status = result.Outcome switch
        {
          TestOutcome.Passed => "✓ Pass".Green(),
          TestOutcome.Failed => "X Fail".Red(),
          TestOutcome.Skipped => "⚠ Skip".Yellow(),
          _ => result.Outcome.ToString()
        };

        string duration = $"{result.Duration.TotalSeconds:F2}s";

        string message = result.Outcome switch
        {
          TestOutcome.Passed => "Completed successfully",
          TestOutcome.Skipped => result.FailureMessage ?? "Skipped",
          TestOutcome.Failed => result.FailureMessage ?? "Failed",
          _ => string.Empty
        };

        // Truncate long messages
        if (message.Length > maxMessageWidth)
        {
          message = string.Concat(message.AsSpan(0, maxMessageWidth - 3), "...");
        }

        table.AddRow(FormatTestName(result.TestName), status, duration, message);
      }
    });

    terminal.WriteLine();

    // Summary line with colors
    terminal.WriteLine($"{"Total:".Bold()} {summary.TotalTests}");
    terminal.WriteLine($"{"Passed:".Green()} {summary.PassedCount}");
    if (summary.FailedCount > 0)
    {
      terminal.WriteLine($"{"Failed:".Red()} {summary.FailedCount}");
    }
    if (summary.SkippedCount > 0)
    {
      terminal.WriteLine($"{"Skipped:".Yellow()} {summary.SkippedCount}");
    }
  }

  /// <summary>
  /// Prints a summary table for multiple test class results.
  /// </summary>
  /// <param name="summary">The test suite summary containing all class results.</param>
  /// <param name="terminal">Optional terminal for output. Uses NuruTerminal if not specified.</param>
  public static void PrintSuiteSummaryTable(TestSuiteSummary summary, ITerminal? terminal = null)
  {
    ArgumentNullException.ThrowIfNull(summary);
    terminal ??= new TimeWarpTerminal();

    terminal.WriteLine("Test Suite Summary".Bold());
    terminal.WriteLine(new string('=', 60));

    terminal.WriteTable(table =>
    {
      table
        .AddColumn("Class")
        .AddColumn("Passed", Alignment.Right)
        .AddColumn("Failed", Alignment.Right)
        .AddColumn("Skipped", Alignment.Right)
        .AddColumn("Total", Alignment.Right)
        .AddColumn("Duration", Alignment.Right)
        .Border(BorderStyle.Rounded);

      foreach (TestRunSummary classResult in summary.ClassResults)
      {
        string passedText = classResult.PassedCount.ToString(CultureInfo.InvariantCulture);
        string failedText = classResult.FailedCount > 0
          ? classResult.FailedCount.ToString(CultureInfo.InvariantCulture).Red()
          : classResult.FailedCount.ToString(CultureInfo.InvariantCulture);
        string skippedText = classResult.SkippedCount > 0
          ? classResult.SkippedCount.ToString(CultureInfo.InvariantCulture).Yellow()
          : classResult.SkippedCount.ToString(CultureInfo.InvariantCulture);
        string duration = $"{classResult.TotalDuration.TotalSeconds:F2}s";

        table.AddRow(
          classResult.ClassName,
          passedText.Green(),
          failedText,
          skippedText,
          classResult.TotalTests.ToString(CultureInfo.InvariantCulture),
          duration
        );
      }
    });

    terminal.WriteLine();

    // Overall summary
    string overallStatus = summary.Success
      ? "ALL TESTS PASSED".Green().Bold()
      : "TESTS FAILED".Red().Bold();

    terminal.WriteLine($"{"Overall:".Bold()} {overallStatus}");
    terminal.WriteLine($"{"Total Tests:".Bold()} {summary.TotalTests}");
    terminal.WriteLine($"{"Passed:".Green()} {summary.PassedCount}");
    if (summary.FailedCount > 0)
    {
      terminal.WriteLine($"{"Failed:".Red()} {summary.FailedCount}");
    }
    if (summary.SkippedCount > 0)
    {
      terminal.WriteLine($"{"Skipped:".Yellow()} {summary.SkippedCount}");
    }
    terminal.WriteLine($"{"Duration:".Bold()} {summary.TotalDuration.TotalSeconds:F2}s");
  }

  [GeneratedRegex("([A-Z])")]
  private static partial Regex PascalCaseRegex();
}
