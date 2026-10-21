#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Locks [Input] node identity: distinct stable uids, and discovery counts that match the run.
#endregion

#region Design
// Fixtures are not registered. The meta-tests drive them through DiscoverTestCases and RunTestsAsync
// so a failing row is an expected result, not a suite failure.
// A bad once-hook fails the same nodes discovery lists, including one node for a skipped
// or tag-mismatched method that also has [Input] rows.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class InputIdentity_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<InputIdentity_Given_>();

    public static async Task SeveralShapes_Should_MatchDiscoveryToRun()
    {
      List<string> discovered = [.. TestRunner.DiscoverTestCases(typeof(ShapeFixture)).Select(testCase => testCase.Uid)];
      List<string> again = [.. TestRunner.DiscoverTestCases(typeof(ShapeFixture)).Select(testCase => testCase.Uid)];
      again.ShouldBe(discovered);

      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ShapeFixture>(sink);
      List<string> ran = [.. sink.Results.Select(result => result.Uid)];

      ran.ShouldBe(discovered);
      stats.TotalTests.ShouldBe(discovered.Count);
      stats.FailedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(4);
      stats.SkippedCount.ShouldBe(1);

      string prefix = $"{typeof(ShapeFixture).FullName}.";
      discovered.ShouldContain($"{prefix}Plain");
      discovered.ShouldNotContain($"{prefix}Plain[0]");
      discovered.ShouldContain($"{prefix}Duplicates[0]");
      discovered.ShouldContain($"{prefix}Duplicates[1]");
      discovered.ShouldContain($"{prefix}Skipped");
      discovered.ShouldNotContain($"{prefix}Skipped[0]");
      discovered.ShouldContain($"{prefix}FailThenPass[0]");
      discovered.ShouldContain($"{prefix}FailThenPass[1]");

      TestNodeInfo first = sink.Results.Single(result => result.Uid.EndsWith("FailThenPass[0]", StringComparison.Ordinal));
      TestNodeInfo second = sink.Results.Single(result => result.Uid.EndsWith("FailThenPass[1]", StringComparison.Ordinal));
      // Awaited async exceptions are recorded as Error. FailedCount still includes them.
      first.State.ShouldBe(TestNodeState.Error);
      second.State.ShouldBe(TestNodeState.Passed);
      first.Uid.ShouldNotBe(second.Uid);
    }

    public static async Task SelectedOrdinal_Should_RunOnlyThatRow()
    {
      CollectingSink sink = new();
      string failingUid = $"{typeof(FailThenPassFixture).FullName}.Case[0]";
      TestRunStats stats = await TestRunner.RunTestsAsync<FailThenPassFixture>(
        sink,
        casePredicate: testCase => testCase.Uid == failingUid);

      stats.TotalTests.ShouldBe(1);
      stats.FailedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(0);
      sink.Results.Single().Uid.ShouldBe(failingUid);
      sink.Results.Single().State.ShouldBe(TestNodeState.Error);
    }

    public static async Task BadSetupOnce_Should_FailEachInputRow()
    {
      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BadSetupOnceFixture>(sink);

      stats.FailedCount.ShouldBe(2);
      stats.PassedCount.ShouldBe(0);
      sink.Results.Count.ShouldBe(2);
      sink.Results[0].Uid.ShouldBe($"{typeof(BadSetupOnceFixture).FullName}.Case[0]");
      sink.Results[1].Uid.ShouldBe($"{typeof(BadSetupOnceFixture).FullName}.Case[1]");
      sink.Results[0].State.ShouldBe(TestNodeState.Failed);
      sink.Results[1].State.ShouldBe(TestNodeState.Failed);
      sink.Results[0].Uid.ShouldNotBe(sink.Results[1].Uid);
    }

    public static async Task BadHook_Should_MatchDiscoveryForSkippedAndTaggedInputs()
    {
      const string filterTag = "Wanted";
      Type fixture = typeof(BadHookSkippedInputsFixture);
      List<string> discovered =
      [
        .. TestRunner.DiscoverTestCases(fixture, filterTag).Select(testCase => testCase.Uid)
      ];

      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync(fixture, sink, filterTag);
      List<string> ran = [.. sink.Results.Select(result => result.Uid)];

      ran.ShouldBe(discovered);
      stats.FailedCount.ShouldBe(discovered.Count);
      stats.PassedCount.ShouldBe(0);
      stats.SkippedCount.ShouldBe(0);

      string prefix = $"{fixture.FullName}.";
      string skippedUid = $"{prefix}Skipped";
      string taggedUid = $"{prefix}Tagged";
      discovered.ShouldBe(
      [
        skippedUid,
        taggedUid,
        $"{prefix}Live[0]",
        $"{prefix}Live[1]"
      ]);
      sink.Results.Single(result => result.Uid == skippedUid).State.ShouldBe(TestNodeState.Failed);
      sink.Results.Single(result => result.Uid == taggedUid).State.ShouldBe(TestNodeState.Failed);

      CollectingSink selected = new();
      TestRunStats selectedStats = await TestRunner.RunTestsAsync(
        fixture,
        selected,
        filterTag,
        casePredicate: testCase => testCase.Uid == skippedUid);
      selectedStats.TotalTests.ShouldBe(1);
      selectedStats.FailedCount.ShouldBe(1);
      selected.Results.Single().Uid.ShouldBe(skippedUid);

      CollectingSink expanded = new();
      TestRunStats expandedStats = await TestRunner.RunTestsAsync(
        fixture,
        expanded,
        filterTag,
        casePredicate: testCase => testCase.Uid == $"{skippedUid}[0]");
      expandedStats.TotalTests.ShouldBe(0);
      expanded.Results.ShouldBeEmpty();
    }

    /// <summary>Collects completed nodes. Local so this file runs on its own.</summary>
    private sealed class CollectingSink : ITestResultSink
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

    private sealed class ShapeFixture
    {
      public static async Task Plain()
      {
        await Task.CompletedTask;
      }

      [Input("same")]
      [Input("same")]
      public static async Task Duplicates(string value)
      {
        _ = value;
        await Task.CompletedTask;
      }

      [Skip("row stays one node")]
      [Input("a")]
      [Input("b")]
      public static async Task Skipped(string value)
      {
        _ = value;
        await Task.CompletedTask;
      }

      [Input("fail")]
      [Input("pass")]
      public static async Task FailThenPass(string value)
      {
        if (value == "fail")
        {
          throw new InvalidOperationException("first input fails");
        }

        await Task.CompletedTask;
      }
    }

    private sealed class FailThenPassFixture
    {
      [Input("fail")]
      [Input("pass")]
      public static async Task Case(string value)
      {
        if (value == "fail")
        {
          throw new InvalidOperationException("first input fails");
        }

        await Task.CompletedTask;
      }
    }

    private sealed class BadSetupOnceFixture
    {
      public static Task SetupOnce(int unused)
      {
        _ = unused;
        return Task.CompletedTask;
      }

      [Input(1)]
      [Input(2)]
      public static async Task Case(int value)
      {
        _ = value;
        await Task.CompletedTask;
      }
    }

    private sealed class BadHookSkippedInputsFixture
    {
      public static Task SetupOnce(int unused)
      {
        _ = unused;
        return Task.CompletedTask;
      }

      [Skip("one node")]
      [Input("a")]
      [Input("b")]
      public static async Task Skipped(string value)
      {
        _ = value;
        await Task.CompletedTask;
      }

      [TestTag("Other")]
      [Input("a")]
      [Input("b")]
      public static async Task Tagged(string value)
      {
        _ = value;
        await Task.CompletedTask;
      }

      [Input(1)]
      [Input(2)]
      public static async Task Live(int value)
      {
        _ = value;
        await Task.CompletedTask;
      }
    }
  }
}
