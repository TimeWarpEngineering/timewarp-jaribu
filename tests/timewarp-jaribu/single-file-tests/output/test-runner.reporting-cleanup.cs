#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests for test runner reporting output and cleanup lifecycle hooks
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Reporting_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Reporting_Given_.MixedResults_Should_CountPassesAndFailure
//
// Fixtures are not registered. Reporting_Given_ asserts stats and CleanUp counts.
// A class with no qualifying methods reports success and does not invoke CleanUp,
// because CleanUp runs from each executed test, not from an empty discovery list.
// Runner pass/fail counters are per RunTestsAsync call.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  sealed class ReportingCollectingSink : ITestResultSink
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

  [TestTag("Output")]
  public class Reporting_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Reporting_Given_>();

    public static async Task MixedResults_Should_CountPassesAndFailure()
    {
      ReportingMixedFixture.Reset();
      ReportingCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ReportingMixedFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.PassedCount.ShouldBe(2);
      stats.FailedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(3);
      stats.ClassName.ShouldBe(nameof(ReportingMixedFixture));
      ReportingMixedFixture.CleanUpCount.ShouldBe(3);
      ReportingMixedFixture.PassCount.ShouldBe(2);

      TestNodeInfo failed = sink.Results.Single(node =>
        node.DisplayName == nameof(ReportingMixedFixture.MixedResults_Should_CountFailingTest));
      failed.State.ShouldBe(TestNodeState.Error);
      failed.Exception.ShouldNotBeNull();
      failed.Exception.ShouldBeOfType<InvalidOperationException>();
      failed.Exception.Message.ShouldContain("REPORT-01");
    }

    public static async Task MultipleRuns_Should_NotAccumulateCounters()
    {
      ReportingMixedFixture.Reset();
      ReportingCollectingSink firstSink = new();
      TestRunStats first = await TestRunner.RunTestsAsync<ReportingMixedFixture>(firstSink);
      ReportingCollectingSink secondSink = new();
      TestRunStats second = await TestRunner.RunTestsAsync<ReportingMixedFixture>(secondSink);

      first.PassedCount.ShouldBe(2);
      first.FailedCount.ShouldBe(1);
      second.PassedCount.ShouldBe(2);
      second.FailedCount.ShouldBe(1);
      ReportingMixedFixture.CleanUpCount.ShouldBe(6);
      ReportingMixedFixture.PassCount.ShouldBe(4);
    }

    public static async Task FilteredContext_Should_RunUntaggedAndSkipTagged()
    {
      ReportingFilterFixture.Reset();
      ReportingCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ReportingFilterFixture>(sink, filterTag: "feature");

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(1);
      ReportingFilterFixture.UntaggedRuns.ShouldBe(1);
      ReportingFilterFixture.TaggedRuns.ShouldBe(0);

      TestNodeInfo skipped = sink.Results.Single(node => node.State == TestNodeState.Skipped);
      skipped.DisplayName.ShouldBe(nameof(ReportingFilterFixture.TaggedMethod_Should_SkipWhenUnmatched));
      skipped.Message.ShouldBe("No matching tag 'feature'");
    }

    public static async Task EmptyClass_Should_ReportZeroAndSkipCleanUp()
    {
      ReportingEmptyClassFixture.CleanUpCount = 0;
      ReportingCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ReportingEmptyClassFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.TotalTests.ShouldBe(0);
      stats.PassedCount.ShouldBe(0);
      stats.FailedCount.ShouldBe(0);
      stats.ClassName.ShouldBe(nameof(ReportingEmptyClassFixture));
      ReportingEmptyClassFixture.CleanUpCount.ShouldBe(0);
      sink.Results.ShouldBeEmpty();
    }
  }

  public class ReportingMixedFixture
  {
    public static int CleanUpCount;
    public static int PassCount;

    public static void Reset()
    {
      CleanUpCount = 0;
      PassCount = 0;
    }

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }

    public static async Task MixedResults_Should_CountPassingTest1()
    {
      PassCount++;
      await Task.CompletedTask;
    }

    public static async Task MixedResults_Should_CountPassingTest2()
    {
      PassCount++;
      await Task.CompletedTask;
    }

    public static async Task MixedResults_Should_CountFailingTest()
    {
      await Task.CompletedTask;
      throw new InvalidOperationException("Intentional fail for REPORT-01");
    }
  }

  public class ReportingFilterFixture
  {
    public static int UntaggedRuns;
    public static int TaggedRuns;

    public static void Reset()
    {
      UntaggedRuns = 0;
      TaggedRuns = 0;
    }

    public static async Task FilteredContext_Should_RunPassingTest()
    {
      UntaggedRuns++;
      await Task.CompletedTask;
    }

    [TestTag("other")]
    public static async Task TaggedMethod_Should_SkipWhenUnmatched()
    {
      TaggedRuns++;
      await Task.CompletedTask;
    }
  }

  public class ReportingEmptyClassFixture
  {
    public static int CleanUpCount;

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }
  }
}
