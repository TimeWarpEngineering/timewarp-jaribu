#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Covers discovery of open generics, the RunAllTests registration snapshot,
// and Cancelled counting as a failing state.
#endregion

#region Design
// The snapshot meta-test swaps the live registration list and calls RunAllTests.
// It restores the previous list before returning. The outer run already holds
// its own snapshot, so the swap does not drop later classes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  sealed class RunnerGuardsCollectingSink : ITestResultSink
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
  public class RunnerGuards_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RunnerGuards_Given_>();

    public static async Task OpenGenericMethod_Should_BeSkippedByDiscovery()
    {
      List<string> names =
      [
        .. TestRunner.DiscoverTests(typeof(OpenGenericDiscoveryFixture)).Select(method => method.Name)
      ];

      names.ShouldContain("ClosedMethod_Should_Run");
      names.ShouldNotContain("OpenMethod_Should_NotBeDiscovered");

      RunnerGuardsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<OpenGenericDiscoveryFixture>(sink);
      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(1);
    }

    public static async Task RunAllTests_Should_IgnoreRegistrationEditsDuringRun()
    {
      Type[] saved = [.. TestRunner.RegisteredTestClasses];
      RegistrationSnapshotSentinel.Runs = 0;
      RegistrationSnapshotOther.Runs = 0;
      try
      {
        TestRunner.ClearRegisteredTests();
        TestRunner.RegisterTests<RegistrationSnapshotSentinel>();
        TestRunner.RegisterTests<RegistrationSnapshotMutator>();

        int exitCode = await TestRunner.RunAllTests();

        exitCode.ShouldBe(0);
        RegistrationSnapshotSentinel.Runs.ShouldBe(1);
        RegistrationSnapshotOther.Runs.ShouldBe(0);
      }
      finally
      {
        TestRunner.ClearRegisteredTests();
        System.Reflection.MethodInfo register = typeof(TestRunner)
          .GetMethod(nameof(TestRunner.RegisterTests))!;
        foreach (Type type in saved)
        {
          register.MakeGenericMethod(type).Invoke(null, null);
        }
      }
    }

    public static async Task Cancelled_Should_CountAsFailure()
    {
      TestRunStats.IsFailingState(TestNodeState.Cancelled).ShouldBeTrue();
      TestRunStats.IsFailingState(TestNodeState.Failed).ShouldBeTrue();
      TestRunStats.IsFailingState(TestNodeState.Error).ShouldBeTrue();
      TestRunStats.IsFailingState(TestNodeState.Timeout).ShouldBeTrue();
      TestRunStats.IsFailingState(TestNodeState.Passed).ShouldBeFalse();
      TestRunStats.IsFailingState(TestNodeState.Skipped).ShouldBeFalse();

      TestNodeInfo[] nodes =
      [
        new("passed", "passed", TestNodeState.Passed),
        new("cancelled", "cancelled", TestNodeState.Cancelled)
      ];
      int passedCount = nodes.Count(node => node.State == TestNodeState.Passed);
      int failedCount = nodes.Count(node => TestRunStats.IsFailingState(node.State));
      int skippedCount = nodes.Count(node => node.State == TestNodeState.Skipped);
      TestRunStats stats = new(
        "Cancelled",
        DateTimeOffset.Now,
        TimeSpan.Zero,
        passedCount,
        failedCount,
        skippedCount);
      stats.FailedCount.ShouldBe(1);
      stats.Success.ShouldBeFalse();
      stats.TotalTests.ShouldBe(2);
      await Task.CompletedTask;
    }
  }

  public class OpenGenericDiscoveryFixture
  {
    public static async Task ClosedMethod_Should_Run()
    {
      await Task.CompletedTask;
    }

    public static async Task OpenMethod_Should_NotBeDiscovered<T>()
    {
      await Task.CompletedTask;
    }
  }

  public class RegistrationSnapshotSentinel
  {
    public static int Runs;

    public static async Task OnlyTest()
    {
      Runs++;
      await Task.CompletedTask;
    }
  }

  public class RegistrationSnapshotMutator
  {
    public static async Task OnlyTest()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<RegistrationSnapshotOther>();
      await Task.CompletedTask;
    }
  }

  public class RegistrationSnapshotOther
  {
    public static int Runs;

    public static async Task OnlyTest()
    {
      Runs++;
      await Task.CompletedTask;
    }
  }
}
