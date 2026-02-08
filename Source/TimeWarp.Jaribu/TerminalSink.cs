namespace TimeWarp.Jaribu;

using System.Globalization;
using TimeWarp.Terminal;

/// <summary>
/// Pretty console output sink using TimeWarp.Terminal for colored, formatted test results.
/// </summary>
public sealed class TerminalSink : ITestResultSink
{
  private readonly ITerminal Terminal;
  private readonly int MaxMessageWidth;

  /// <summary>
  /// Creates a new TerminalSink with the specified terminal and message width.
  /// </summary>
  /// <param name="terminal">The terminal to write output to.</param>
  /// <param name="maxMessageWidth">Maximum width for message column before truncation. Default 50.</param>
  public TerminalSink(ITerminal terminal, int maxMessageWidth = 50)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    MaxMessageWidth = maxMessageWidth;
  }

  /// <summary>
  /// Creates a new TerminalSink using the default TimeWarpTerminal.
  /// </summary>
  public TerminalSink() : this(new TimeWarpTerminal()) { }

  public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;

  public Task OnTestStartedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
#pragma warning disable CA1849
    Terminal.WriteLine($"Test: {TestHelpers.FormatTestName(node.DisplayName)}");
#pragma warning restore CA1849
    return Task.CompletedTask;
  }

  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    string line = node.State switch
    {
      TestNodeState.Passed => "  ✓ PASSED",
      TestNodeState.Failed => $"  ✗ FAILED: {node.Message ?? node.Exception?.Message ?? "Unknown"}",
      TestNodeState.Skipped => $"  ⚠ SKIPPED: {node.Message ?? "No reason"}",
      TestNodeState.Timeout => $"  ✗ TIMEOUT: {node.Message ?? node.Exception?.Message ?? "Exceeded limit"}",
      TestNodeState.Error => $"  ✗ ERROR: {node.Message ?? node.Exception?.Message ?? "Unknown"}",
      TestNodeState.Cancelled => "  ⚠ CANCELLED",
      _ => $"  ? {node.State}"
    };
#pragma warning disable CA1849
    Terminal.WriteLine(line);
    Terminal.WriteLine(string.Empty);
#pragma warning restore CA1849
    return Task.CompletedTask;
  }

  public Task OnRunStartedAsync(string className, string? filterTag = null)
  {
#pragma warning disable CA1849
    Terminal.WriteLine($"🧪 Testing {className}...");
    if (filterTag is not null)
    {
      Terminal.WriteLine($"   (filtered by tag: {filterTag})");
    }
    Terminal.WriteLine(string.Empty);
#pragma warning restore CA1849
    return Task.CompletedTask;
  }

  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results)
  {
    ArgumentNullException.ThrowIfNull(stats);
#pragma warning disable CA1849
    Terminal.WriteTable(table =>
    {
      table
        .AddColumn("Test")
        .AddColumn("Status")
        .AddColumn("Duration", Alignment.Right)
        .AddColumn("Message")
        .Border(BorderStyle.Rounded);

      foreach (TestNodeInfo node in results)
      {
        string status = node.State switch
        {
          TestNodeState.Passed => "✓ Pass".Green(),
          TestNodeState.Failed => "X Fail".Red(),
          TestNodeState.Skipped => "⚠ Skip".Yellow(),
          TestNodeState.Timeout => "⏱ Timeout".Red(),
          TestNodeState.Error => "✗ Error".Red(),
          TestNodeState.Cancelled => "⚠ Cancel".Yellow(),
          _ => node.State.ToString()
        };

        string duration = node.Duration.HasValue
          ? $"{node.Duration.Value.TotalSeconds:F2}s"
          : "-";

        string message = node.State switch
        {
          TestNodeState.Passed => "Completed successfully",
          TestNodeState.Skipped => node.Message ?? "Skipped",
          TestNodeState.Failed => node.Message ?? node.Exception?.Message ?? "Failed",
          TestNodeState.Timeout => node.Message ?? "Timeout",
          TestNodeState.Error => node.Message ?? node.Exception?.Message ?? "Error",
          TestNodeState.Cancelled => "Cancelled",
          _ => string.Empty
        };

        if (message.Length > MaxMessageWidth)
        {
          message = string.Concat(message.AsSpan(0, MaxMessageWidth - 3), "...");
        }

        table.AddRow(TestHelpers.FormatTestName(node.DisplayName), status, duration, message);
      }
    });

    Terminal.WriteLine(string.Empty);
    Terminal.WriteLine($"{"Total:".Bold()} {stats.TotalTests.ToString(CultureInfo.InvariantCulture)}");
    Terminal.WriteLine($"{"Passed:".Green()} {stats.PassedCount.ToString(CultureInfo.InvariantCulture)}");
    if (stats.FailedCount > 0)
    {
      Terminal.WriteLine($"{"Failed:".Red()} {stats.FailedCount.ToString(CultureInfo.InvariantCulture)}");
    }
    if (stats.SkippedCount > 0)
    {
      Terminal.WriteLine($"{"Skipped:".Yellow()} {stats.SkippedCount.ToString(CultureInfo.InvariantCulture)}");
    }
#pragma warning restore CA1849

    return Task.CompletedTask;
  }
}
