#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

// This is a meta-test file that tests the tabular output formatting.
// It uses mock data and TestTerminal to verify TerminalSink output directly.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

/// <summary>
/// Tests for the tabular output formatting in TerminalSink.OnRunCompletedAsync.
/// </summary>
[TestTag("Jaribu")]
public class TabularOutputTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TabularOutputTests>();

  public static async Task TableStructureWithTestTerminal()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();
    using TerminalSink sink = new(terminal);

    List<TestNodeInfo> results =
    [
      new("NS.Class.PassingTest", "PassingTest", TestNodeState.Passed, TimeSpan.FromMilliseconds(150)),
      new("NS.Class.FailingTest", "FailingTest", TestNodeState.Failed, TimeSpan.FromMilliseconds(250), Exception: new ArgumentException("Invalid value"), Message: "ArgumentException: Invalid value"),
      new("NS.Class.SkippedTest", "SkippedTest", TestNodeState.Skipped, TimeSpan.Zero, Message: "Not implemented yet")
    ];

    TestRunStats stats = new(
      "TestClass",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(400),
      PassedCount: 1,
      FailedCount: 1,
      SkippedCount: 1
    );

    await sink.OnRunCompletedAsync(stats, results);

    string output = terminal.Output;

    // Verify table borders (rounded style)
    output.ShouldContain("╭");
    output.ShouldContain("╰");

    // Verify column headers
    output.ShouldContain("Test");
    output.ShouldContain("Status");
    output.ShouldContain("Duration");
    output.ShouldContain("Message");

    // Verify test names are formatted (PascalCase to spaces)
    output.ShouldContain("Passing Test");
    output.ShouldContain("Failing Test");
    output.ShouldContain("Skipped Test");

    // Verify status text
    output.ShouldContain("Pass");
    output.ShouldContain("Fail");
    output.ShouldContain("Skip");

    // Verify summary totals
    output.ShouldContain("Total:");
    output.ShouldContain("Passed:");
    output.ShouldContain("Failed:");
    output.ShouldContain("Skipped:");
  }

  public static async Task LongMessageTruncation()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();
    using TerminalSink sink = new(terminal, maxMessageWidth: 30);

    string longMessage = "This is a very long error message that should be truncated because it exceeds the maximum width limit";

    List<TestNodeInfo> results =
    [
      new("NS.Class.LongMessageTest", "LongMessageTest", TestNodeState.Failed, TimeSpan.FromMilliseconds(100), Message: longMessage)
    ];

    TestRunStats stats = new(
      "TestClass",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(100),
      PassedCount: 0,
      FailedCount: 1,
      SkippedCount: 0
    );

    await sink.OnRunCompletedAsync(stats, results);

    string output = terminal.Output;

    // Should contain truncation indicator
    output.ShouldContain("...");

    // Should NOT contain the full message
    output.ShouldNotContain("maximum width limit");
  }

  public static async Task AnsiColorCodesInOutput()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();
    using TerminalSink sink = new(terminal);

    List<TestNodeInfo> results =
    [
      new("NS.Class.GreenTest", "GreenTest", TestNodeState.Passed, TimeSpan.FromMilliseconds(50)),
      new("NS.Class.RedTest", "RedTest", TestNodeState.Failed, TimeSpan.FromMilliseconds(50), Message: "Error"),
      new("NS.Class.YellowTest", "YellowTest", TestNodeState.Skipped, TimeSpan.Zero, Message: "Skipped")
    ];

    TestRunStats stats = new(
      "ColorTest",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(100),
      PassedCount: 1,
      FailedCount: 1,
      SkippedCount: 1
    );

    await sink.OnRunCompletedAsync(stats, results);

    string output = terminal.Output;

    // Check for ANSI color codes (32=green, 31=red, 33=yellow)
    (output.Contains("\u001b[32m") || output.Contains("[32m")).ShouldBeTrue();
    (output.Contains("\u001b[31m") || output.Contains("[31m")).ShouldBeTrue();
    (output.Contains("\u001b[33m") || output.Contains("[33m")).ShouldBeTrue();
  }

  public static async Task DurationFormatting()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();
    using TerminalSink sink = new(terminal);

    List<TestNodeInfo> results =
    [
      new("NS.Class.QuickTest", "QuickTest", TestNodeState.Passed, TimeSpan.FromMilliseconds(5)),
      new("NS.Class.SlowTest", "SlowTest", TestNodeState.Passed, TimeSpan.FromSeconds(2.5))
    ];

    TestRunStats stats = new(
      "DurationTest",
      DateTimeOffset.Now,
      TimeSpan.FromSeconds(2.505),
      PassedCount: 2,
      FailedCount: 0,
      SkippedCount: 0
    );

    await sink.OnRunCompletedAsync(stats, results);

    string output = terminal.Output;

    // Should contain formatted durations with 's' suffix
    (output.Contains("0.01s") || output.Contains("0.00s")).ShouldBeTrue();
    output.ShouldContain("2.50s");
  }

  public static async Task EmptyResultsHandling()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();
    using TerminalSink sink = new(terminal);

    TestRunStats stats = new(
      "EmptyTest",
      DateTimeOffset.Now,
      TimeSpan.Zero,
      PassedCount: 0,
      FailedCount: 0,
      SkippedCount: 0
    );

    await sink.OnRunCompletedAsync(stats, []);

    string output = terminal.Output;

    // Should still render table structure
    output.ShouldContain("Test");
    output.ShouldContain("Status");

    // Should show Total: 0
    output.ShouldContain("Total:");
    output.ShouldContain("0");
  }
}
