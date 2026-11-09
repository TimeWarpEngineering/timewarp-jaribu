#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Covers discovery of open generics, the RunAllTests registration snapshot,
// Cancelled counting as a failing state, null guards on sinks and attributes,
// and a rejected attribute reported as an Error node, including a class tag miss.
#endregion

#region Design
// The snapshot meta-test swaps the live registration list and calls RunAllTests.
// It restores the previous list before returning. RunAllTests and the MTP host
// each hold their own snapshot, so the swap does not drop later classes.
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

    public static async Task NullArguments_Should_Throw()
    {
      Should.Throw<ArgumentNullException>(() => new SkipAttribute(null!));
      new SkipAttribute(string.Empty).Reason.ShouldBe(string.Empty);
      new SkipAttribute(" ").Reason.ShouldBe(" ");
      Should.Throw<ArgumentNullException>(() => new TestTagAttribute(null!));
      new TestTagAttribute(string.Empty).Tag.ShouldBe(string.Empty);
      new TestTagAttribute(" ").Tag.ShouldBe(" ");
      new InputAttribute(null).Parameters.ShouldBe([null]);
      InputAttribute row = new(null, 1);
      row.Parameters.Length.ShouldBe(2);
      row.Parameters[0].ShouldBeNull();

      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnTestDiscoveredAsync(null!));
      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnTestStartedAsync(null!));
      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnTestCompletedAsync(null!));
      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnRunStartedAsync(null!));
      TestRunStats stats = new("C", DateTimeOffset.UnixEpoch, TimeSpan.Zero, 0, 0, 0);
      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnRunCompletedAsync(null!, []));
      Should.Throw<ArgumentNullException>(() => NullSink.Instance.OnRunCompletedAsync(stats, null!));

      using TerminalSink sink = new();
      Should.Throw<ArgumentNullException>(() => sink.OnTestDiscoveredAsync(null!));
      Should.Throw<ArgumentNullException>(() => sink.OnRunStartedAsync(null!));
      Should.Throw<ArgumentNullException>(() => sink.OnRunCompletedAsync(stats, null!));
      await Task.CompletedTask;
    }

    public static async Task RejectedAttribute_Should_FailTheRun()
    {
      RejectedAttributeFixture.GoodRuns = 0;
      RunnerGuardsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<RejectedAttributeFixture>(sink);
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBe(2);
      stats.SkippedCount.ShouldBe(1);
      stats.Success.ShouldBeFalse();
      RejectedAttributeFixture.GoodRuns.ShouldBe(1);

      TestNodeInfo timeoutError = sink.Results.Single(
        node => node.DisplayName == nameof(RejectedAttributeFixture.ZeroTimeout_Should_NotRun));
      timeoutError.State.ShouldBe(TestNodeState.Error);
      timeoutError.Message.ShouldNotBeNull();
      timeoutError.Message.ShouldContain(nameof(TimeoutAttribute));

      TestNodeInfo skipError = sink.Results.Single(
        node => node.DisplayName == nameof(RejectedAttributeFixture.NullSkip_Should_NotRun));
      skipError.State.ShouldBe(TestNodeState.Error);
      skipError.Message.ShouldNotBeNull();
      skipError.Message.ShouldContain(nameof(SkipAttribute));

      TestNodeInfo skipped = sink.Results.Single(node => node.State == TestNodeState.Skipped);
      skipped.DisplayName.ShouldBe(nameof(RejectedAttributeFixture.EmptySkip_Should_Skip));
      skipped.Message.ShouldBe(string.Empty);

      System.Reflection.MethodInfo zeroTimeout = typeof(RejectedAttributeFixture)
        .GetMethod(nameof(RejectedAttributeFixture.ZeroTimeout_Should_NotRun))!;
      TestNodeInfo single = await TestRunner.RunSingleTestAsync(typeof(RejectedAttributeFixture), zeroTimeout);
      single.State.ShouldBe(TestNodeState.Error);
      single.Message.ShouldNotBeNull();
      single.Message.ShouldContain(nameof(TimeoutAttribute));

      int exitCode = await TestRunner.RunTests<RejectedAttributeFixture>();
      exitCode.ShouldBe(1);
      RejectedAttributeFixture.GoodRuns.ShouldBe(2);

      ClassTimeoutFixture.Runs = 0;
      RunnerGuardsCollectingSink classSink = new();
      TestRunStats classStats = await TestRunner.RunTestsAsync<ClassTimeoutFixture>(classSink);
      classStats.Success.ShouldBeFalse();
      classStats.PassedCount.ShouldBe(0);
      classStats.FailedCount.ShouldBe(1);
      ClassTimeoutFixture.Runs.ShouldBe(0);
      TestNodeInfo classError = classSink.Results.Single();
      classError.State.ShouldBe(TestNodeState.Error);
      classError.Message.ShouldNotBeNull();
      classError.Message.ShouldContain(nameof(TimeoutAttribute));
      classError.Message.ShouldContain("the class");
    }

    public static async Task RejectedAttribute_OnTagMiss_Should_FailTheRun()
    {
      ClassTagMissRejectionFixture.Runs = 0;
      MethodTagMissRejectionFixture.Runs = 0;
      RunnerGuardsCollectingSink classSink = new();
      TestRunStats classStats = await TestRunner.RunTestsAsync<ClassTagMissRejectionFixture>(
        classSink,
        filterTag: "B");
      classStats.Success.ShouldBeFalse();
      classStats.PassedCount.ShouldBe(0);
      classStats.FailedCount.ShouldBe(1);
      classStats.SkippedCount.ShouldBe(1);
      ClassTagMissRejectionFixture.Runs.ShouldBe(0);

      TestNodeInfo classError = classSink.Results.Single(
        node => node.DisplayName == nameof(ClassTagMissRejectionFixture.ZeroTimeout_Should_NotRun));
      classError.State.ShouldBe(TestNodeState.Error);
      classError.Message.ShouldNotBeNull();
      classError.Message.ShouldContain(nameof(TimeoutAttribute));

      TestNodeInfo classSkip = classSink.Results.Single(node => node.State == TestNodeState.Skipped);
      classSkip.DisplayName.ShouldBe(nameof(ClassTagMissRejectionFixture.Sibling_Should_Skip));
      classSkip.Message.ShouldBe("No matching tag 'B'");

      int classExit = await TestRunner.RunTests<ClassTagMissRejectionFixture>(filterTag: "B");
      classExit.ShouldBe(1);
      ClassTagMissRejectionFixture.Runs.ShouldBe(0);

      RunnerGuardsCollectingSink methodSink = new();
      TestRunStats methodStats = await TestRunner.RunTestsAsync<MethodTagMissRejectionFixture>(
        methodSink,
        filterTag: "B");
      methodStats.Success.ShouldBeFalse();
      methodStats.PassedCount.ShouldBe(1);
      methodStats.FailedCount.ShouldBe(1);
      methodStats.SkippedCount.ShouldBe(0);
      MethodTagMissRejectionFixture.Runs.ShouldBe(1);

      TestNodeInfo methodError = methodSink.Results.Single(
        node => node.DisplayName == nameof(MethodTagMissRejectionFixture.ZeroTimeout_Should_NotRun));
      methodError.State.ShouldBe(TestNodeState.Error);
      methodError.Message.ShouldNotBeNull();
      methodError.Message.ShouldContain(nameof(TimeoutAttribute));

      TestNodeInfo methodPass = methodSink.Results.Single(node => node.State == TestNodeState.Passed);
      methodPass.DisplayName.ShouldBe(nameof(MethodTagMissRejectionFixture.Sibling_Should_Pass));

      int methodExit = await TestRunner.RunTests<MethodTagMissRejectionFixture>(filterTag: "B");
      methodExit.ShouldBe(1);
      MethodTagMissRejectionFixture.Runs.ShouldBe(2);
    }

#if JARIBU_MULTI
    public static async Task RegisteredSuites_Should_IncludeCiSafeMetaTests()
    {
      string[] names = [.. TestRunner.RegisteredTestClasses.Select(type => type.Name)];
      names.ShouldContain(nameof(MultiClassRegistration_Given_));
      names.ShouldContain(nameof(Discovery_Given_));
      names.ShouldContain(nameof(SkipExceptions_Given_));
      names.ShouldContain(nameof(Parameterized_Given_));
      names.ShouldContain(nameof(Edges_Given_));
      names.ShouldContain(nameof(Reporting_Given_));
      await Task.CompletedTask;
    }
#endif
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

// Attribute arguments cannot use the null-forgiving operator. Null is the value under test.
#nullable disable
  public class RejectedAttributeFixture
  {
    public static int GoodRuns;

    public static async Task Good_Should_Pass()
    {
      GoodRuns++;
      await Task.CompletedTask;
    }

    [Skip("")]
    public static async Task EmptySkip_Should_Skip()
    {
      throw new InvalidOperationException("empty skip must not run");
    }

    [Skip(null)]
    public static async Task NullSkip_Should_NotRun()
    {
      throw new InvalidOperationException("null skip must not run");
    }

    [Timeout(0)]
    public static async Task ZeroTimeout_Should_NotRun()
    {
      throw new InvalidOperationException("rejected timeout must not run");
    }
  }
#nullable restore

  [Timeout(0)]
  public class ClassTimeoutFixture
  {
    public static int Runs;

    public static async Task Good_Should_NotRun()
    {
      Runs++;
      await Task.CompletedTask;
    }
  }

  [TestTag("A")]
  public class ClassTagMissRejectionFixture
  {
    public static int Runs;

    public static async Task SetupOnce()
    {
      Runs++;
      await Task.CompletedTask;
    }

    public static async Task Sibling_Should_Skip()
    {
      Runs++;
      await Task.CompletedTask;
    }

    [Timeout(0)]
    public static async Task ZeroTimeout_Should_NotRun()
    {
      Runs++;
      await Task.CompletedTask;
    }
  }

  public class MethodTagMissRejectionFixture
  {
    public static int Runs;

    public static async Task Sibling_Should_Pass()
    {
      Runs++;
      await Task.CompletedTask;
    }

    [TestTag("A")]
    [Timeout(0)]
    public static async Task ZeroTimeout_Should_NotRun()
    {
      Runs++;
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
