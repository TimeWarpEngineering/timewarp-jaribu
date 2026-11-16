namespace TimeWarp.Jaribu;

using System.Globalization;
using TimeWarp.Terminal;

#region Purpose
// Ranks tautology findings worst first and prints one line per test.
#endregion

/// <summary>Ranking and console text for tautology findings.</summary>
public static class TautologyReport
{
  /// <summary>
  /// Orders violations first, then fewer kills per second, then slower tests, then test id.
  /// </summary>
  /// <param name="findings">Findings from one run. Null is rejected.</param>
  /// <returns>A new list. The input order is left unchanged.</returns>
  public static IReadOnlyList<TautologyFinding> Rank(IReadOnlyList<TautologyFinding> findings)
  {
    ArgumentNullException.ThrowIfNull(findings);
    return
    [
      .. findings
        .OrderByDescending(finding => finding.Violation)
        .ThenBy(finding => finding.KillsPerSecond)
        .ThenByDescending(finding => finding.Seconds)
        .ThenBy(finding => finding.TestId, StringComparer.Ordinal)
    ];
  }

  /// <summary>
  /// One report line: test id, rule or claim, reason, file:line, and the score.
  /// </summary>
  /// <param name="finding">The finding to format. Null is rejected.</param>
  public static string Format(TautologyFinding finding)
  {
    ArgumentNullException.ThrowIfNull(finding);
    string kills = double.IsPositiveInfinity(finding.KillsPerSecond)
      ? "inf"
      : finding.KillsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
    string seconds = finding.Seconds.ToString("F3", CultureInfo.InvariantCulture);
    return string.Join(
      " | ",
      finding.TestId,
      finding.Rule,
      finding.Reason,
      finding.Location,
      "covered=" + finding.Covered.ToString(CultureInfo.InvariantCulture),
      "proven=" + finding.Proven.ToString(CultureInfo.InvariantCulture),
      "time=" + seconds + "s",
      "kills/s=" + kills);
  }

  /// <summary>
  /// Prints the ranked report. An empty list prints nothing.
  /// </summary>
  /// <param name="terminal">Destination. Null is rejected.</param>
  /// <param name="findings">Findings from the run. Null is rejected.</param>
  public static void Write(ITerminal terminal, IReadOnlyList<TautologyFinding> findings)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(findings);
    if (findings.Count == 0)
    {
      return;
    }

    terminal.WriteLine(string.Empty);
    terminal.WriteLine("Tautology report (worst first)");
    foreach (TautologyFinding finding in Rank(findings))
    {
      terminal.WriteLine(Format(finding));
    }
  }
}
