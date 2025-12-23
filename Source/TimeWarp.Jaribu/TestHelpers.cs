namespace TimeWarp.Jaribu;

using System;
using System.Globalization;
using System.IO;
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
  /// Clears the runfile cache entry(ies) for a specific file to ensure fresh compilation on the current run.
  /// Deletes top-level cache dirs prefixed with the filename (e.g., "jaribu-05-cache-clearing-<hash>").
  /// </summary>
  /// <param name="filePath">Full path to the file (e.g., .cs script).</param>
  /// <param name="deleteAllPrefixed">If true, deletes all matching prefixed dirs (default: true, for completeness).</param>
  public static void ClearRunfileCache(string filePath, bool deleteAllPrefixed = true)
  {
    string runfileCacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot) || !File.Exists(filePath))
    {
        return;
    }

    string filePrefix = Path.GetFileNameWithoutExtension(filePath).ToUpperInvariant() + "-";
    bool clearedAny = false;

    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
        string cacheDirName = Path.GetFileName(cacheDir).ToUpperInvariant();
        if (cacheDirName.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Directory.Delete(cacheDir, recursive: true);
                Console.WriteLine($"✓ Cleared runfile cache for {Path.GetFileName(filePath)}: {Path.GetFileName(cacheDir)}");
                clearedAny = true;

                if (!deleteAllPrefixed)
                {
                    return; // Stop after first match (if not deleting all)
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"⚠ Skipped clearing {Path.GetFileName(cacheDir)} (locked/in use): {ex.Message}");
                // Continue to next; don't fail the whole op
            }
        }
    }

    if (!clearedAny)
    {
        Console.WriteLine($"⚠ No runfile cache prefixed with '{filePrefix}' found for {Path.GetFileName(filePath)}; proceeding.");
    }
  }

  /// <summary>
  /// Clears all runfile caches (broad fallback, as in original TestRunner).
  /// </summary>
  public static void ClearAllRunfileCaches()
  {
    string runfileCacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot))
    {
        return;
    }

    bool anyDeleted = false;
    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
        try
        {
            Directory.Delete(cacheDir, recursive: true);
            if (!anyDeleted)
            {
                Console.WriteLine("✓ Clearing all runfile caches:");
                anyDeleted = true;
            }

            Console.WriteLine($"  - {Path.GetFileName(cacheDir)}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Console.WriteLine($"  - Skipped {Path.GetFileName(cacheDir)} (locked): {ex.Message}");
        }
    }

    if (anyDeleted)
    {
        Console.WriteLine();
    }
  }

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

    Table table = new Table()
      .AddColumn("Test")
      .AddColumn("Status")
      .AddColumn("Duration", Alignment.Right)
      .AddColumn("Message");

    table.Border = BorderStyle.Rounded;

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

    terminal.WriteTable(table);
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

    Table table = new Table()
      .AddColumn("Class")
      .AddColumn("Passed", Alignment.Right)
      .AddColumn("Failed", Alignment.Right)
      .AddColumn("Skipped", Alignment.Right)
      .AddColumn("Total", Alignment.Right)
      .AddColumn("Duration", Alignment.Right);

    table.Border = BorderStyle.Rounded;

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

    terminal.WriteTable(table);
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
