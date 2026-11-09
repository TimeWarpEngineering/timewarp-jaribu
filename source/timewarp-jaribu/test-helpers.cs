namespace TimeWarp.Jaribu;

using System.Text.RegularExpressions;
using TimeWarp.Terminal;

#region Purpose
// Formats test names for terminal output.
#endregion

#region Design
// Spaces go before capitals in the method name only. Text from the first
// '(' onward is the argument list and stays as written, so Method(Foo, Bar)
// does not gain a space before each capital inside the parentheses.
#endregion

/// <summary>
/// Helper utilities for test formatting and common patterns.
/// </summary>
public static partial class TestHelpers
{
  /// <summary>
  /// Converts a PascalCase test method name to a readable form.
  /// Example: "CatchAllInOptionShouldFail" → "Catch All In Option Should Fail".
  /// An argument list after the first '(' is left unchanged.
  /// </summary>
  public static string FormatTestName(string name)
  {
    ArgumentNullException.ThrowIfNull(name);
    int split = name.IndexOf('(', StringComparison.Ordinal);
    if (split < 0)
    {
      return SpacePascalCase(name);
    }

    return string.Concat(SpacePascalCase(name[..split]), name[split..]);
  }

  /// <summary>
  /// Logs a test pass status to <paramref name="terminal"/>.
  /// </summary>
  public static void TestPassed(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    terminal.WriteLine("  ✓ PASSED");
  }

  /// <summary>
  /// Logs a test failure status with reason to <paramref name="terminal"/>.
  /// </summary>
  public static void TestFailed(ITerminal terminal, string reason)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    terminal.WriteLine($"  ✗ FAILED: {reason}");
  }

  /// <summary>
  /// Logs a test skipped status with reason to <paramref name="terminal"/>.
  /// </summary>
  public static void TestSkipped(ITerminal terminal, string reason)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    terminal.WriteLine($"  ⚠ SKIPPED: {reason}");
  }

  private static string SpacePascalCase(string name) =>
    PascalCaseRegex().Replace(name, " $1").Trim();

  [GeneratedRegex("([A-Z])")]
  private static partial Regex PascalCaseRegex();
}
