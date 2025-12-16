#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

// This is a meta-test file that tests the RunTestsWithResults API itself.
// It uses custom validation logic rather than the standard test pattern.
// In multi-mode, it registers StructuredResultsMetaTests which wraps the validation.

#if !JARIBU_MULTI
RegisterTests<StructuredResultsMetaTests>();
return await RunAllTests();
#endif

/// <summary>
/// Meta-tests that validate the RunTestsWithResults API by running test classes
/// and validating their structured output.
/// </summary>
[TestTag("Jaribu")]
public class StructuredResultsMetaTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<StructuredResultsMetaTests>();

  public static async Task BasicStructuredResultsFromMixedPassFailSkip()
  {
    TestRunSummary summary = await TestRunner.RunTestsWithResults<MixedResultsTests>();

    summary.ClassName.ShouldBe("MixedResults");
    summary.PassedCount.ShouldBe(1);
    summary.FailedCount.ShouldBe(1);
    summary.SkippedCount.ShouldBe(1);
    summary.TotalTests.ShouldBe(3);
    summary.Success.ShouldBeFalse();
    summary.Results.Count.ShouldBe(3);
    summary.TotalDuration.ShouldBeGreaterThan(TimeSpan.Zero);
  }

  public static async Task IndividualTestResultDetails()
  {
    TestRunSummary summary = await TestRunner.RunTestsWithResults<MixedResultsTests>();

    TestResult? passedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Passed);
    passedResult.ShouldNotBeNull();
    passedResult.TestName.ShouldBe("PassingTest");
    passedResult.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    passedResult.FailureMessage.ShouldBeNull();

    TestResult? failedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Failed);
    failedResult.ShouldNotBeNull();
    failedResult.TestName.ShouldBe("FailingTest");
    failedResult.FailureMessage.ShouldNotBeNullOrEmpty();
    failedResult.StackTrace.ShouldNotBeNullOrEmpty();

    TestResult? skippedResult = summary.Results.FirstOrDefault(r => r.Outcome == TestOutcome.Skipped);
    skippedResult.ShouldNotBeNull();
    skippedResult.TestName.ShouldBe("SkippedTest");
    skippedResult.FailureMessage.ShouldNotBeNullOrEmpty();
  }

  public static async Task ParameterizedTestResults()
  {
    TestRunSummary paramSummary = await TestRunner.RunTestsWithResults<ParameterizedResultTests>();

    paramSummary.Results.Count.ShouldBe(2);

    foreach (TestResult result in paramSummary.Results)
    {
      result.Parameters.ShouldNotBeNull();
      result.Parameters.Count.ShouldBeGreaterThan(0);
    }
  }

  public static async Task AllPassingTestsSuccessProperty()
  {
    TestRunSummary allPassSummary = await TestRunner.RunTestsWithResults<AllPassingResultTests>();

    allPassSummary.Success.ShouldBeTrue();
    allPassSummary.FailedCount.ShouldBe(0);
  }

  public static async Task StartTimeVerification()
  {
    DateTimeOffset beforeTest = DateTimeOffset.Now;
    TestRunSummary timeSummary = await TestRunner.RunTestsWithResults<AllPassingResultTests>();
    DateTimeOffset afterTest = DateTimeOffset.Now;

    timeSummary.StartTime.ShouldBeGreaterThanOrEqualTo(beforeTest);
    timeSummary.StartTime.ShouldBeLessThanOrEqualTo(afterTest);
  }
}

// Test class with mixed results (used by meta-tests above)
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

// Test class with parameterized tests (used by meta-tests above)
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

// Test class with all passing tests (used by meta-tests above)
[TestTag("Jaribu")]
public class AllPassingResultTests
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
