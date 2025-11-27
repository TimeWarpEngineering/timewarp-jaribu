#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

using TimeWarp.Jaribu;
using static System.Console;

// Test the new RunTestsWithResults method
await TestStructuredResults();

async Task TestStructuredResults()
{
  WriteLine("=== Testing RunTestsWithResults<T>() ===");
  WriteLine();

  // Test 1: Basic structured results
  WriteLine("Test 1: Basic structured results from mixed pass/fail/skip");
  TestRunSummary summary = await TestRunner.RunTestsWithResults<MixedResultsTests>();

  // Verify the summary
  bool test1Passed = true;

  if (summary.ClassName != "MixedResults")
  {
    WriteLine($"  ✗ ClassName expected 'MixedResults', got '{summary.ClassName}'");
    test1Passed = false;
  }

  if (summary.PassedCount != 1)
  {
    WriteLine($"  ✗ PassedCount expected 1, got {summary.PassedCount}");
    test1Passed = false;
  }

  if (summary.FailedCount != 1)
  {
    WriteLine($"  ✗ FailedCount expected 1, got {summary.FailedCount}");
    test1Passed = false;
  }

  if (summary.SkippedCount != 1)
  {
    WriteLine($"  ✗ SkippedCount expected 1, got {summary.SkippedCount}");
    test1Passed = false;
  }

  if (summary.TotalTests != 3)
  {
    WriteLine($"  ✗ TotalTests expected 3, got {summary.TotalTests}");
    test1Passed = false;
  }

  if (summary.Success)
  {
    WriteLine($"  ✗ Success expected false (has failures), got true");
    test1Passed = false;
  }

  if (summary.Results.Count != 3)
  {
    WriteLine($"  ✗ Results.Count expected 3, got {summary.Results.Count}");
    test1Passed = false;
  }

  if (summary.TotalDuration <= TimeSpan.Zero)
  {
    WriteLine($"  ✗ TotalDuration should be > 0, got {summary.TotalDuration}");
    test1Passed = false;
  }

  if (test1Passed)
  {
    WriteLine("  ✓ Test 1 PASSED: Summary counts correct");
  }

  WriteLine();

  // Test 2: Verify individual TestResult details
  WriteLine("Test 2: Verify individual TestResult details");
  bool test2Passed = true;

  TestResult? passedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Passed);
  if (passedResult is null)
  {
    WriteLine("  ✗ No passed result found");
    test2Passed = false;
  }
  else
  {
    if (passedResult.TestName != "PassingTest")
    {
      WriteLine($"  ✗ Passed test name expected 'PassingTest', got '{passedResult.TestName}'");
      test2Passed = false;
    }
    if (passedResult.Duration <= TimeSpan.Zero)
    {
      WriteLine($"  ✗ Passed test duration should be > 0");
      test2Passed = false;
    }
    if (passedResult.FailureMessage is not null)
    {
      WriteLine($"  ✗ Passed test should have null FailureMessage");
      test2Passed = false;
    }
  }

  TestResult? failedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Failed);
  if (failedResult is null)
  {
    WriteLine("  ✗ No failed result found");
    test2Passed = false;
  }
  else
  {
    if (failedResult.TestName != "FailingTest")
    {
      WriteLine($"  ✗ Failed test name expected 'FailingTest', got '{failedResult.TestName}'");
      test2Passed = false;
    }
    if (string.IsNullOrEmpty(failedResult.FailureMessage))
    {
      WriteLine($"  ✗ Failed test should have FailureMessage");
      test2Passed = false;
    }
    if (string.IsNullOrEmpty(failedResult.StackTrace))
    {
      WriteLine($"  ✗ Failed test should have StackTrace");
      test2Passed = false;
    }
  }

  TestResult? skippedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Skipped);
  if (skippedResult is null)
  {
    WriteLine("  ✗ No skipped result found");
    test2Passed = false;
  }
  else
  {
    if (skippedResult.TestName != "SkippedTest")
    {
      WriteLine($"  ✗ Skipped test name expected 'SkippedTest', got '{skippedResult.TestName}'");
      test2Passed = false;
    }
    if (string.IsNullOrEmpty(skippedResult.FailureMessage))
    {
      WriteLine($"  ✗ Skipped test should have FailureMessage (skip reason)");
      test2Passed = false;
    }
  }

  if (test2Passed)
  {
    WriteLine("  ✓ Test 2 PASSED: Individual result details correct");
  }

  WriteLine();

  // Test 3: Parameterized test results
  WriteLine("Test 3: Parameterized test results");
  TestRunSummary paramSummary = await TestRunner.RunTestsWithResults<ParameterizedResultTests>();
  bool test3Passed = true;

  if (paramSummary.Results.Count != 2)
  {
    WriteLine($"  ✗ Expected 2 results for parameterized test, got {paramSummary.Results.Count}");
    test3Passed = false;
  }

  foreach (TestResult result in paramSummary.Results)
  {
    if (result.Parameters is null || result.Parameters.Count == 0)
    {
      WriteLine($"  ✗ Result '{result.TestName}' should have Parameters");
      test3Passed = false;
    }
  }

  if (test3Passed)
  {
    WriteLine("  ✓ Test 3 PASSED: Parameterized results have parameters");
  }

  WriteLine();

  // Test 4: All passing tests - Success should be true
  WriteLine("Test 4: All passing tests - Success property");
  TestRunSummary allPassSummary = await TestRunner.RunTestsWithResults<AllPassingTests>();
  bool test4Passed = true;

  if (!allPassSummary.Success)
  {
    WriteLine($"  ✗ Success expected true for all passing, got false");
    test4Passed = false;
  }

  if (allPassSummary.FailedCount != 0)
  {
    WriteLine($"  ✗ FailedCount expected 0, got {allPassSummary.FailedCount}");
    test4Passed = false;
  }

  if (test4Passed)
  {
    WriteLine("  ✓ Test 4 PASSED: All passing tests have Success=true");
  }

  WriteLine();

  // Test 5: StartTime is set
  WriteLine("Test 5: StartTime verification");
  DateTimeOffset beforeTest = DateTimeOffset.Now;
  TestRunSummary timeSummary = await TestRunner.RunTestsWithResults<AllPassingTests>();
  DateTimeOffset afterTest = DateTimeOffset.Now;
  bool test5Passed = true;

  if (timeSummary.StartTime < beforeTest || timeSummary.StartTime > afterTest)
  {
    WriteLine($"  ✗ StartTime {timeSummary.StartTime} not within expected range");
    test5Passed = false;
  }

  if (test5Passed)
  {
    WriteLine("  ✓ Test 5 PASSED: StartTime is correctly set");
  }

  WriteLine();
  WriteLine("=== Structured Results Tests Complete ===");
}

// Test class with mixed results
[TestTag("Jaribu")]
public class MixedResultsTests
{
  public static async Task PassingTest()
  {
    await Task.CompletedTask;
  }

  public static async Task FailingTest()
  {
    await Task.CompletedTask;
    throw new InvalidOperationException("Intentional failure for testing");
  }

  [Skip("Testing skip functionality")]
  public static async Task SkippedTest()
  {
    await Task.CompletedTask;
  }
}

// Test class with parameterized tests
[TestTag("Jaribu")]
public class ParameterizedResultTests
{
  [Input("value1", 1)]
  [Input("value2", 2)]
  public static async Task ParamTest(string name, int number)
  {
    WriteLine($"  ParamTest: {name}, {number}");
    await Task.CompletedTask;
  }
}

// Test class with all passing tests
[TestTag("Jaribu")]
public class AllPassingTests
{
  public static async Task Test1()
  {
    await Task.CompletedTask;
  }

  public static async Task Test2()
  {
    await Task.CompletedTask;
  }
}
