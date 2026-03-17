namespace TimeWarp.Jaribu;

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
  /// Logs a test pass status.
  /// </summary>
  public static void TestPassed() =>
    Terminal.WriteLine("  ✓ PASSED");

  /// <summary>
  /// Logs a test failure status with reason.
  /// </summary>
  public static void TestFailed(string reason) =>
    Terminal.WriteLine($"  ✗ FAILED: {reason}");

  /// <summary>
  /// Logs a test skipped status with reason.
  /// </summary>
  public static void TestSkipped(string reason) =>
    Terminal.WriteLine($"  ⚠ SKIPPED: {reason}");

  [GeneratedRegex("([A-Z])")]
  private static partial Regex PascalCaseRegex();
}
