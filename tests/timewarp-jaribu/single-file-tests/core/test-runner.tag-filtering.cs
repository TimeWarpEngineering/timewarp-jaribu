#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests tag-based filtering of test methods including matching, mismatching,
// untagged, case-insensitive, multi-tag, and environment variable filtering.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (TagFiltering_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.TagFiltering_Given_.MatchingTag_Should_Run
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class TagFiltering_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TagFiltering_Given_>();

    /// <summary>
    /// Method with matching tag - should run when filter="feature1".
    /// </summary>
    [TestTag("feature1")]
    public static async Task MatchingTag_Should_Run()
    {
      WriteLine("MatchingTag_Should_Run: Running");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Method with mismatched tag - should skip when filter="feature1".
    /// </summary>
    [TestTag("other")]
    public static async Task MismatchedTag_Should_Skip()
    {
      WriteLine("MismatchedTag_Should_Skip: Should not run");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Untagged method in filtered run - should run (implicit match).
    /// </summary>
    public static async Task UntaggedMethod_Should_RunImplicitly()
    {
      WriteLine("UntaggedMethod_Should_RunImplicitly: Running (implicit)");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Case-insensitive matching - tag "Feature1" vs filter "feature1".
    /// </summary>
    [TestTag("Feature1")]
    public static async Task CaseInsensitiveTag_Should_Match()
    {
      WriteLine("CaseInsensitiveTag_Should_Match: Running");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Multiple tags on method - should match if any matches filter.
    /// </summary>
    [TestTag("feature1")]
    [TestTag("extra")]
    public static async Task MultipleTags_Should_MatchAny()
    {
      WriteLine("MultipleTags_Should_MatchAny: Running (multiple tags)");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Method for env var filtering test.
    /// </summary>
    [TestTag("envtag")]
    public static async Task EnvVarFilter_Should_Match()
    {
      WriteLine("EnvVarFilter_Should_Match: Running with env filter");
      await Task.CompletedTask;
    }

    /// <summary>
    /// methodNameContains selection omits non-matching methods (not Skipped).
    /// </summary>
    public static async Task MethodNameContains_Should_OmitNonMatches()
    {
      MethodFilterFixture.Reset();
      MethodFilterCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<MethodFilterFixture>(
        sink,
        methodNameContains: "Alpha");

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(0);
      MethodFilterFixture.AlphaCount.ShouldBe(1);
      MethodFilterFixture.BetaCount.ShouldBe(0);
      sink.Results.Count.ShouldBe(1);
      sink.Results[0].DisplayName.ShouldBe(nameof(MethodFilterFixture.AlphaTest));
    }

    /// <summary>
    /// methodNameContains is case-insensitive substring match.
    /// </summary>
    public static async Task MethodNameContains_Should_MatchCaseInsensitiveSubstring()
    {
      MethodFilterFixture.Reset();
      MethodFilterCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<MethodFilterFixture>(
        sink,
        methodNameContains: "beta");

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      MethodFilterFixture.AlphaCount.ShouldBe(0);
      MethodFilterFixture.BetaCount.ShouldBe(1);
    }

    /// <summary>
    /// methodPredicate omits non-matching methods (selection; no Skipped nodes).
    /// Used by MTP uid/tree filter on the run path.
    /// </summary>
    public static async Task MethodPredicate_Should_OmitNonMatchesWithoutSkippedNodes()
    {
      MethodFilterFixture.Reset();
      MethodFilterCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<MethodFilterFixture>(
        sink,
        methodPredicate: method => method.Name == nameof(MethodFilterFixture.AlphaTest));

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(0);
      MethodFilterFixture.AlphaCount.ShouldBe(1);
      MethodFilterFixture.BetaCount.ShouldBe(0);
      sink.Results.Count.ShouldBe(1);
      sink.Results[0].DisplayName.ShouldBe(nameof(MethodFilterFixture.AlphaTest));
    }
  }

  sealed class MethodFilterCollectingSink : ITestResultSink
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

  public class MethodFilterFixture
  {
    public static int AlphaCount;
    public static int BetaCount;

    public static void Reset()
    {
      AlphaCount = 0;
      BetaCount = 0;
    }

    public static async Task AlphaTest()
    {
      AlphaCount++;
      await Task.CompletedTask;
    }

    public static async Task BetaTest()
    {
      BetaCount++;
      await Task.CompletedTask;
    }
  }
}
