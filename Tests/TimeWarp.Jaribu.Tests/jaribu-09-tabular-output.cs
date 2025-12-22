#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

// This is a meta-test file that tests the tabular output formatting.
// It uses mock data and TestTerminal to verify output without running actual tests.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

/// <summary>
/// Tests for the tabular output formatting in TestHelpers.PrintResultsTable.
/// </summary>
[TestTag("Jaribu")]
public class TabularOutputTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TabularOutputTests>();

  public static async Task TableStructureWithTestTerminal()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();

    // Create a summary manually to test PrintResultsTable directly
    var results = new List<TestResult>
    {
      new("PassingTest", TestOutcome.Passed, TimeSpan.FromMilliseconds(150), null, null, null),
      new("FailingTest", TestOutcome.Failed, TimeSpan.FromMilliseconds(250), "ArgumentException: Invalid value", "at Test.Method()", null),
      new("SkippedTest", TestOutcome.Skipped, TimeSpan.Zero, "Not implemented yet", null, null)
    };

    var summary = new TestRunSummary(
      "TestClass",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(400),
      PassedCount: 1,
      FailedCount: 1,
      SkippedCount: 1,
      results
    );

    TestHelpers.PrintResultsTable(summary, terminal);

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

    await Task.CompletedTask;
  }

  public static async Task LongMessageTruncation()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();

    string longMessage = "This is a very long error message that should be truncated because it exceeds the maximum width limit";

    var results = new List<TestResult>
    {
      new("LongMessageTest", TestOutcome.Failed, TimeSpan.FromMilliseconds(100), longMessage, null, null)
    };

    var summary = new TestRunSummary(
      "TestClass",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(100),
      PassedCount: 0,
      FailedCount: 1,
      SkippedCount: 0,
      results
    );

    TestHelpers.PrintResultsTable(summary, terminal, maxMessageWidth: 30);

    string output = terminal.Output;

    // Should contain truncation indicator
    output.ShouldContain("...");

    // Should NOT contain the full message
    output.ShouldNotContain("maximum width limit");

    await Task.CompletedTask;
  }

  public static async Task AnsiColorCodesInOutput()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();

    var results = new List<TestResult>
    {
      new("GreenTest", TestOutcome.Passed, TimeSpan.FromMilliseconds(50), null, null, null),
      new("RedTest", TestOutcome.Failed, TimeSpan.FromMilliseconds(50), "Error", null, null),
      new("YellowTest", TestOutcome.Skipped, TimeSpan.Zero, "Skipped", null, null)
    };

    var summary = new TestRunSummary(
      "ColorTest",
      DateTimeOffset.Now,
      TimeSpan.FromMilliseconds(100),
      PassedCount: 1,
      FailedCount: 1,
      SkippedCount: 1,
      results
    );

    TestHelpers.PrintResultsTable(summary, terminal);

    string output = terminal.Output;

    // Check for ANSI color codes (32=green, 31=red, 33=yellow)
    (output.Contains("\u001b[32m") || output.Contains("[32m")).ShouldBeTrue();
    (output.Contains("\u001b[31m") || output.Contains("[31m")).ShouldBeTrue();
    (output.Contains("\u001b[33m") || output.Contains("[33m")).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task DurationFormatting()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();

    var results = new List<TestResult>
    {
      new("QuickTest", TestOutcome.Passed, TimeSpan.FromMilliseconds(5), null, null, null),
      new("SlowTest", TestOutcome.Passed, TimeSpan.FromSeconds(2.5), null, null, null)
    };

    var summary = new TestRunSummary(
      "DurationTest",
      DateTimeOffset.Now,
      TimeSpan.FromSeconds(2.505),
      PassedCount: 2,
      FailedCount: 0,
      SkippedCount: 0,
      results
    );

    TestHelpers.PrintResultsTable(summary, terminal);

    string output = terminal.Output;

    // Should contain formatted durations with 's' suffix
    (output.Contains("0.01s") || output.Contains("0.00s")).ShouldBeTrue();
    output.ShouldContain("2.50s");

    await Task.CompletedTask;
  }

  public static async Task EmptyResultsHandling()
  {
    using TimeWarp.Terminal.TestTerminal terminal = new();

    var summary = new TestRunSummary(
      "EmptyTest",
      DateTimeOffset.Now,
      TimeSpan.Zero,
      PassedCount: 0,
      FailedCount: 0,
      SkippedCount: 0,
      new List<TestResult>()
    );

    TestHelpers.PrintResultsTable(summary, terminal);

    string output = terminal.Output;

    // Should still render table structure
    output.ShouldContain("Test");
    output.ShouldContain("Status");

    // Should show Total: 0
    output.ShouldContain("Total:");
    output.ShouldContain("0");

    await Task.CompletedTask;
  }
}
