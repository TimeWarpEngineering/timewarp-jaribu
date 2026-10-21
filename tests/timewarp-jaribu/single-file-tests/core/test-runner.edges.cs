#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests edge cases including generic methods, mismatched parameter counts,
// multi-tag no-match, non-qualifying methods, ValueTask, timeout, and cancellation.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Edges_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Edges_Given_.Discovery_Should_OmitNonQualifyingMethods
//
// Fixtures are not ModuleInitializer-registered. The timeout fixture uses 100ms so the
// meta-test proves Timeout without hanging the suite. A CancellationToken parameter is
// discovered and then fails Invoke, because Invoke does not fill optional arguments.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;
  sealed class EdgesCollectingSink : ITestResultSink
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

  [TestTag("Jaribu")]
  public class Edges_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Edges_Given_>();

    public static async Task Discovery_Should_OmitNonQualifyingMethods()
    {
      List<string> names =
      [
        .. TestRunner.DiscoverTests(typeof(EdgesDiscoveryFixture)).Select(method => method.Name)
      ];

      names.ShouldContain(nameof(EdgesDiscoveryFixture.ClosedMethod_Should_Run));
      names.ShouldNotContain(nameof(EdgesDiscoveryFixture.OpenMethod_Should_NotBeDiscovered));
      names.ShouldNotContain(nameof(EdgesDiscoveryFixture.NonQualifyingMethod_Should_BeSkipped));
      names.ShouldNotContain(nameof(EdgesDiscoveryFixture.ValueTask_Should_NotRunUntilSupported));
      names.Count.ShouldBe(1);

      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesDiscoveryFixture>(sink);
      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(1);
    }

    public static async Task MismatchedParamCount_Should_FailWithoutRunningBody()
    {
      EdgesMismatchFixture.BodyRuns = 0;
      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesMismatchFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(0);
      EdgesMismatchFixture.BodyRuns.ShouldBe(0);

      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<TargetParameterCountException>();
    }

    public static async Task MultiTagNoMatch_Should_SkipWhenUnmatched()
    {
      EdgesTagFixture.Runs = 0;
      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesTagFixture>(sink, filterTag: "unrelated");

      stats.Success.ShouldBeTrue();
      stats.SkippedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(0);
      EdgesTagFixture.Runs.ShouldBe(0);
      sink.Results.Single().State.ShouldBe(TestNodeState.Skipped);
      sink.Results.Single().Message.ShouldBe("No matching tag 'unrelated'");
    }

    public static async Task MultiTag_Should_RunWhenAnyTagMatches()
    {
      EdgesTagFixture.Runs = 0;
      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesTagFixture>(sink, filterTag: "no-match2");

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(0);
      EdgesTagFixture.Runs.ShouldBe(1);
    }

    public static async Task HangingTest_Should_Timeout()
    {
      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesTimeoutFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Timeout);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<TimeoutException>();
      node.Message.ShouldNotBeNull();
      node.Message.ShouldContain("Timeout");
      node.Duration.ShouldNotBeNull();
      node.Duration.Value.ShouldBeLessThan(TimeSpan.FromSeconds(1.5));
    }

    public static async Task CancellationToken_Should_FailParameterCount()
    {
      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesCancellationFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<TargetParameterCountException>();
    }

    public static async Task EmptyClass_Should_SucceedWithoutInvokingCleanUp()
    {
      EdgesEmptyFixture.CleanUpCount = 0;
      List<string> names =
      [
        .. TestRunner.DiscoverTests(typeof(EdgesEmptyFixture)).Select(method => method.Name)
      ];
      names.ShouldBeEmpty();

      EdgesCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EdgesEmptyFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.TotalTests.ShouldBe(0);
      stats.PassedCount.ShouldBe(0);
      stats.FailedCount.ShouldBe(0);
      EdgesEmptyFixture.CleanUpCount.ShouldBe(0);
      sink.Results.ShouldBeEmpty();
    }
  }

  public class EdgesDiscoveryFixture
  {
    public static async Task ClosedMethod_Should_Run()
    {
      await Task.CompletedTask;
    }

    public static async Task OpenMethod_Should_NotBeDiscovered<T>()
    {
      await Task.CompletedTask;
    }

    public static void NonQualifyingMethod_Should_BeSkipped()
    {
    }

    private static async Task PrivateMethod_Should_BeSkipped()
    {
      await Task.CompletedTask;
    }

    public static ValueTask ValueTask_Should_NotRunUntilSupported()
    {
      return ValueTask.CompletedTask;
    }
  }

  public class EdgesMismatchFixture
  {
    public static int BodyRuns;

    [Input]
    public static async Task MismatchedParamCount_Should_FailGracefully(string p1, int p2, bool p3)
    {
      BodyRuns++;
      _ = p1;
      _ = p2;
      _ = p3;
      await Task.CompletedTask;
    }
  }

  public class EdgesTagFixture
  {
    public static int Runs;

    [TestTag("no-match1")]
    [TestTag("no-match2")]
    public static async Task MultiTagNoMatch_Should_SkipWhenUnmatched()
    {
      Runs++;
      await Task.CompletedTask;
    }
  }

  public class EdgesTimeoutFixture
  {
    [Timeout(100)]
    public static async Task HangingTest_Should_Timeout()
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
    }
  }

  public class EdgesCancellationFixture
  {
    public static async Task CancellationToken_Should_BeSupported(CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();
      await Task.CompletedTask;
    }
  }

  public class EdgesEmptyFixture
  {
    public static int CleanUpCount;

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }

    public static void NonQualifyingMethod_Should_BeSkipped()
    {
    }

    private static async Task PrivateMethod_Should_BeSkipped()
    {
      await Task.CompletedTask;
    }

    public static ValueTask ValueTask_Should_NotRunUntilSupported()
    {
      return ValueTask.CompletedTask;
    }
  }
}
