namespace TimeWarp.Jaribu;

using System.Globalization;
using TimeWarp.Terminal;

/// <summary>
/// Pretty console output sink using TimeWarp.Terminal for colored, formatted test results.
/// </summary>
public sealed class TerminalSink : ITestResultSink, IDisposable
{
  private const int MinimumMessageWidth = 3;

  private readonly ITerminal Terminal;
  private readonly int MaxMessageWidth;
  private readonly bool OwnsTerminal;
  private bool IsDisposed;

  /// <summary>
  /// Creates a TerminalSink that writes to a caller-owned terminal.
  /// Disposing the sink does not dispose <paramref name="terminal"/>.
  /// </summary>
  /// <param name="terminal">The terminal to write output to. The caller retains ownership.</param>
  /// <param name="maxMessageWidth">
  /// Maximum width for the message column before truncation. Values below 3 are raised to 3
  /// so the ellipsis fits.
  /// </param>
  public TerminalSink(ITerminal terminal, int maxMessageWidth = 50)
    : this(terminal, ownsTerminal: false, maxMessageWidth)
  {
  }

  /// <summary>
  /// Creates a TerminalSink that owns a new <see cref="TimeWarpTerminal"/> and disposes it.
  /// </summary>
  public TerminalSink()
    : this(new TimeWarpTerminal(), ownsTerminal: true, maxMessageWidth: 50)
  {
  }

  private TerminalSink(ITerminal terminal, bool ownsTerminal, int maxMessageWidth)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    OwnsTerminal = ownsTerminal;
    MaxMessageWidth = Math.Max(maxMessageWidth, MinimumMessageWidth);
  }

  /// <summary>
  /// Disposes the sink. The terminal is disposed only when this sink created it.
  /// </summary>
  public void Dispose()
  {
    if (IsDisposed)
    {
      return;
    }

    if (OwnsTerminal && Terminal is IDisposable disposable)
    {
      disposable.Dispose();
    }

    IsDisposed = true;
  }

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
      TestNodeState.Discovered => "  · DISCOVERED",
      TestNodeState.InProgress => "  … IN PROGRESS",
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
          TestNodeState.Discovered => "· Disc",
          TestNodeState.InProgress => "… Run",
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
          TestNodeState.Discovered => "Discovered",
          TestNodeState.InProgress => "In progress",
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
