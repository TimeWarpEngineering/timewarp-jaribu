#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests for the RunTestsAsync sink-based API: structured result collection and stats
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (StructuredResults_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.StructuredResults_Given_.MixedPassFailSkip_Should_ReturnCorrectStats
//
// Uses CollectingSink to capture individual test results and validates
// both aggregated stats and individual test node details.
// Helper classes (CollectingSink, MixedResultsTests, etc.) are test fixtures.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  /// <summary>
  /// A test sink that collects all completed test results for inspection.
  /// </summary>
  sealed class CollectingSink : ITestResultSink
  {
    public List<TestNodeInfo> Results { get; } = [];

    public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
    public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestCompletedAsync(TestNodeInfo node)
    {
      Results.Add(node);
      return Task.CompletedTask;
    }

    public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;
    public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
  }

  [TestTag("Api")]
  public class StructuredResults_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<StructuredResults_Given_>();

    public static async Task MixedPassFailSkip_Should_ReturnCorrectStats()
    {
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<MixedResultsTests>(sink);

      stats.ClassName.ShouldBe("MixedResults");
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(3);
      stats.Success.ShouldBeFalse();
      sink.Results.Count.ShouldBe(3);
      stats.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    public static async Task MixedPassFailSkip_Should_ReturnIndividualDetails()
    {
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<MixedResultsTests>(sink);

      TestNodeInfo? passedResult = sink.Results.FirstOrDefault(r => r.State == TestNodeState.Passed);
      passedResult.ShouldNotBeNull();
      passedResult.DisplayName.ShouldBe("PassingTest");
      passedResult.Duration.ShouldNotBeNull();
      passedResult.Duration.Value.ShouldBeGreaterThan(TimeSpan.Zero);
      passedResult.Message.ShouldBeNull();

      TestNodeInfo? failedResult = sink.Results.FirstOrDefault(r => r.State is TestNodeState.Failed or TestNodeState.Error);
      failedResult.ShouldNotBeNull();
      failedResult.DisplayName.ShouldBe("FailingTest");
      failedResult.Message.ShouldNotBeNullOrEmpty();
      failedResult.Exception.ShouldNotBeNull();

      TestNodeInfo? skippedResult = sink.Results.FirstOrDefault(r => r.State == TestNodeState.Skipped);
      skippedResult.ShouldNotBeNull();
      skippedResult.DisplayName.ShouldBe("SkippedTest");
      skippedResult.Message.ShouldNotBeNullOrEmpty();
    }

    public static async Task ParameterizedTests_Should_ReturnParameterDetails()
    {
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ParameterizedResultTests>(sink);

      sink.Results.Count.ShouldBe(2);

      foreach (TestNodeInfo result in sink.Results)
      {
        result.Parameters.ShouldNotBeNull();
        result.Parameters.Count.ShouldBeGreaterThan(0);
      }
    }

    public static async Task AllPassing_Should_SetSuccessTrue()
    {
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<AllPassingResultTests>(sink);

      stats.Success.ShouldBeTrue();
      stats.FailedCount.ShouldBe(0);
    }

    public static async Task AllPassing_Should_RecordValidStartTime()
    {
      DateTimeOffset beforeTest = DateTimeOffset.Now;
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<AllPassingResultTests>(sink);
      DateTimeOffset afterTest = DateTimeOffset.Now;

      stats.StartTime.ShouldBeGreaterThanOrEqualTo(beforeTest);
      stats.StartTime.ShouldBeLessThanOrEqualTo(afterTest);
    }
  }

  // Test class with mixed results (used by meta-tests above)
  [TestTag("Api")]
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
  [TestTag("Api")]
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
  [TestTag("Api")]
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
}
