#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests that the test runner discovers, executes, and filters methods correctly
// including Setup/CleanUp lifecycle, async methods, and non-qualifying method exclusion.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Discovery_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Discovery_Given_.QualifyingMethods_Should_ExcludeNonTests
//
// The fixture holds the intentional failure. Only Discovery_Given_ is ModuleInitializer-registered,
// and it asserts on RunTestsAsync so the suite itself passes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  sealed class DiscoveryCollectingSink : ITestResultSink
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
  public class Discovery_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Discovery_Given_>();

    public static async Task QualifyingMethods_Should_ExcludeNonTests()
    {
      List<string> names =
      [
        .. TestRunner.DiscoverTests(typeof(DiscoveryLifecycleFixture)).Select(method => method.Name)
      ];

      names.ShouldContain(nameof(DiscoveryLifecycleFixture.BasicMethod_Should_Pass));
      names.ShouldContain(nameof(DiscoveryLifecycleFixture.IntentionalFailure_Should_Fail));
      names.ShouldContain(nameof(DiscoveryLifecycleFixture.SecondPassingMethod_Should_Pass));
      names.ShouldContain(nameof(DiscoveryLifecycleFixture.AsyncAwait_Should_Complete));
      names.ShouldNotContain(nameof(DiscoveryLifecycleFixture.NonAsyncVoidMethod_Should_BeSkipped));
      names.ShouldNotContain(nameof(DiscoveryLifecycleFixture.Setup));
      names.ShouldNotContain(nameof(DiscoveryLifecycleFixture.CleanUp));
      names.Count.ShouldBe(4);
      await Task.CompletedTask;
    }

    public static async Task Lifecycle_Should_CallSetupAndCleanUpAroundEachTest()
    {
      DiscoveryLifecycleFixture.Reset();
      DiscoveryCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<DiscoveryLifecycleFixture>(sink);

      stats.PassedCount.ShouldBe(3);
      stats.FailedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(4);
      DiscoveryLifecycleFixture.SetupCount.ShouldBe(4);
      DiscoveryLifecycleFixture.CleanUpCount.ShouldBe(4);
      DiscoveryLifecycleFixture.PassingCount.ShouldBe(3);
      sink.Results.Count.ShouldBe(4);
    }

    public static async Task Failure_Should_RecordExceptionAndContinue()
    {
      DiscoveryLifecycleFixture.Reset();
      DiscoveryCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<DiscoveryLifecycleFixture>(sink);

      stats.Success.ShouldBeFalse();
      TestNodeInfo failed = sink.Results.Single(node =>
        node.DisplayName == nameof(DiscoveryLifecycleFixture.IntentionalFailure_Should_Fail));
      failed.State.ShouldBe(TestNodeState.Error);
      failed.Exception.ShouldNotBeNull();
      failed.Exception.ShouldBeOfType<ArgumentException>();
      failed.Exception.Message.ShouldContain("Intentional failure");

      sink.Results.Count(node => node.State == TestNodeState.Passed).ShouldBe(3);
    }
  }

  public class DiscoveryLifecycleFixture
  {
    public static int SetupCount;
    public static int CleanUpCount;
    public static int PassingCount;

    public static void Reset()
    {
      SetupCount = 0;
      CleanUpCount = 0;
      PassingCount = 0;
    }

    public static async Task Setup()
    {
      SetupCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }

    public static async Task BasicMethod_Should_Pass()
    {
      PassingCount++;
      await Task.CompletedTask;
    }

    public static void NonAsyncVoidMethod_Should_BeSkipped()
    {
    }

    private static async Task PrivateAsyncMethod_Should_BeSkipped()
    {
      await Task.CompletedTask;
    }

    public static async Task IntentionalFailure_Should_Fail()
    {
      await Task.CompletedTask;
      throw new ArgumentException("Intentional failure");
    }

    public static async Task SecondPassingMethod_Should_Pass()
    {
      PassingCount++;
      await Task.CompletedTask;
    }

    public static async Task AsyncAwait_Should_Complete()
    {
      await Task.Delay(1);
      PassingCount++;
    }
  }
}
