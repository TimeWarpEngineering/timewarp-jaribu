#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

using TimeWarp.Jaribu;
using TimeWarp.Nuru;
using static System.Console;

// Test the tabular output formatting
await TestTabularOutput();

async Task TestTabularOutput()
{
  WriteLine("=== Testing Tabular Output Formatting ===");
  WriteLine();

  // Test 1: Verify table structure is rendered
  WriteLine("Test 1: Table structure with TestTerminal");
  {
    using TestTerminal terminal = new();

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
    bool passed = true;

    // Verify table borders (rounded style)
    if (!output.Contains('╭') || !output.Contains('╰'))
    {
      WriteLine("  ✗ Missing rounded table corners");
      passed = false;
    }

    // Verify column headers
    if (!output.Contains("Test") || !output.Contains("Status") || !output.Contains("Duration") || !output.Contains("Message"))
    {
      WriteLine("  ✗ Missing column headers");
      passed = false;
    }

    // Verify test names are formatted (PascalCase to spaces)
    if (!output.Contains("Passing Test") || !output.Contains("Failing Test") || !output.Contains("Skipped Test"))
    {
      WriteLine("  ✗ Test names not formatted correctly");
      passed = false;
    }

    // Verify status text
    if (!output.Contains("Pass") || !output.Contains("Fail") || !output.Contains("Skip"))
    {
      WriteLine("  ✗ Missing status indicators");
      passed = false;
    }

    // Verify summary totals
    if (!output.Contains("Total:") || !output.Contains("Passed:") || !output.Contains("Failed:") || !output.Contains("Skipped:"))
    {
      WriteLine("  ✗ Missing summary totals");
      passed = false;
    }

    if (passed)
    {
      WriteLine("  ✓ Test 1 PASSED: Table structure correct");
    }
  }

  WriteLine();

  // Test 2: Verify message truncation
  WriteLine("Test 2: Long message truncation");
  {
    using TestTerminal terminal = new();

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
    bool passed = true;

    // Should contain truncation indicator
    if (!output.Contains("..."))
    {
      WriteLine("  ✗ Missing truncation indicator (...)");
      passed = false;
    }

    // Should NOT contain the full message
    if (output.Contains("maximum width limit"))
    {
      WriteLine("  ✗ Message was not truncated");
      passed = false;
    }

    if (passed)
    {
      WriteLine("  ✓ Test 2 PASSED: Long messages truncated correctly");
    }
  }

  WriteLine();

  // Test 3: Verify color codes are present
  WriteLine("Test 3: ANSI color codes in output");
  {
    using TestTerminal terminal = new();

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
    bool passed = true;

    // Check for ANSI color codes (32=green, 31=red, 33=yellow)
    if (!output.Contains("\u001b[32m") && !output.Contains("[32m"))
    {
      WriteLine("  ✗ Missing green ANSI code for passed");
      passed = false;
    }

    if (!output.Contains("\u001b[31m") && !output.Contains("[31m"))
    {
      WriteLine("  ✗ Missing red ANSI code for failed");
      passed = false;
    }

    if (!output.Contains("\u001b[33m") && !output.Contains("[33m"))
    {
      WriteLine("  ✗ Missing yellow ANSI code for skipped");
      passed = false;
    }

    if (passed)
    {
      WriteLine("  ✓ Test 3 PASSED: ANSI color codes present");
    }
  }

  WriteLine();

  // Test 4: Verify duration formatting
  WriteLine("Test 4: Duration formatting");
  {
    using TestTerminal terminal = new();

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
    bool passed = true;

    // Should contain formatted durations with 's' suffix
    if (!output.Contains("0.01s") && !output.Contains("0.00s"))
    {
      WriteLine($"  ✗ Quick test duration not formatted correctly");
      passed = false;
    }

    if (!output.Contains("2.50s"))
    {
      WriteLine("  ✗ Slow test duration not formatted correctly");
      passed = false;
    }

    if (passed)
    {
      WriteLine("  ✓ Test 4 PASSED: Duration formatting correct");
    }
  }

  WriteLine();

  // Test 5: Empty results
  WriteLine("Test 5: Empty results handling");
  {
    using TestTerminal terminal = new();

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
    bool passed = true;

    // Should still render table structure
    if (!output.Contains("Test") || !output.Contains("Status"))
    {
      WriteLine("  ✗ Empty table missing headers");
      passed = false;
    }

    // Should show Total: 0
    if (!output.Contains("Total:") || !output.Contains('0'))
    {
      WriteLine("  ✗ Missing zero total");
      passed = false;
    }

    if (passed)
    {
      WriteLine("  ✓ Test 5 PASSED: Empty results handled correctly");
    }
  }

  WriteLine();
  WriteLine("=== Tabular Output Tests Complete ===");
}
