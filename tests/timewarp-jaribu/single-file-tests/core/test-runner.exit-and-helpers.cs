#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Covers TestHelpers, NullSink, RunTests<T> exit codes, and [Input(null)].
#endregion

#region Design
// Fixtures are nested and unregistered, so a failing row is an expected result.
// RunTests writes through TerminalSink. These tests redirect Terminal.Out so that
// output does not look like a suite failure. Exit codes follow ExitCodeFor:
// 0 when the run succeeded, 1 when a node failed, the class was empty, or the token fired.
// [Input(null)] is one row whose argument is null and whose display name ends in (null).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class ExitAndHelpers_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ExitAndHelpers_Given_>();

    public static async Task FormatTestName_Should_InsertSpacesAtCapitals()
    {
      TestHelpers.FormatTestName("CatchAllInOptionShouldFail")
        .ShouldBe("Catch All In Option Should Fail");
      TestHelpers.FormatTestName("Simple").ShouldBe("Simple");
      await Task.CompletedTask;
    }

    public static async Task StatusLines_Should_NameTheOutcome()
    {
      TextWriter previous = Terminal.Out;
      using StringWriter writer = new();
      Terminal.SetOut(writer);
      try
      {
        TestHelpers.TestPassed();
        TestHelpers.TestFailed("boom");
        TestHelpers.TestSkipped("later");
      }
      finally
      {
        Terminal.SetOut(previous);
      }

      string text = writer.ToString();
      text.ShouldContain("PASSED");
      text.ShouldContain("FAILED: boom");
      text.ShouldContain("SKIPPED: later");
      await Task.CompletedTask;
    }

    public static async Task NullSink_Should_DiscardEventsAndKeepStats()
    {
      NullSink.Instance.ShouldBeSameAs(NullSink.Instance);
      TestNodeInfo node = new("uid", "Name", TestNodeState.Passed);
      Task discovered = NullSink.Instance.OnTestDiscoveredAsync(node);
      Task started = NullSink.Instance.OnTestStartedAsync(node);
      Task completed = NullSink.Instance.OnTestCompletedAsync(node);
      Task runStarted = NullSink.Instance.OnRunStartedAsync("Class", "tag");
      TestRunStats empty = new("Class", DateTimeOffset.UtcNow, TimeSpan.Zero, 0, 0, 0);
      Task runCompleted = NullSink.Instance.OnRunCompletedAsync(empty, []);
      discovered.IsCompletedSuccessfully.ShouldBeTrue();
      started.IsCompletedSuccessfully.ShouldBeTrue();
      completed.IsCompletedSuccessfully.ShouldBeTrue();
      runStarted.IsCompletedSuccessfully.ShouldBeTrue();
      runCompleted.IsCompletedSuccessfully.ShouldBeTrue();

      TestRunStats passed = await TestRunner.RunTestsAsync<PassingExitFixture>(NullSink.Instance);
      passed.Success.ShouldBeTrue();
      passed.PassedCount.ShouldBe(1);

      TestRunStats failed = await TestRunner.RunTestsAsync<FailingExitFixture>(NullSink.Instance);
      failed.Success.ShouldBeFalse();
      failed.FailedCount.ShouldBe(1);
    }

    public static async Task RunTests_Should_MapOutcomesToExitCodes()
    {
      (await RunQuietAsync(() => TestRunner.RunTests<PassingExitFixture>())).ShouldBe(0);
      (await RunQuietAsync(() => TestRunner.RunTests<SkippedExitFixture>())).ShouldBe(0);
      (await RunQuietAsync(() => TestRunner.RunTests<FailingExitFixture>())).ShouldBe(1);
      (await RunQuietAsync(() => TestRunner.RunTests<TimeoutExitFixture>())).ShouldBe(1);
      (await RunQuietAsync(() => TestRunner.RunTests<EmptyExitFixture>())).ShouldBe(1);
      (await RunQuietAsync(() => TestRunner.RunTests<EmptyExitFixture>(allowEmpty: true))).ShouldBe(0);

      using CancellationTokenSource source = new();
      source.Cancel();
      int cancelled = await RunQuietAsync(
        () => TestRunner.RunTests<PassingExitFixture>(cancellationToken: source.Token));
      cancelled.ShouldBe(1);
    }

    public static async Task InputNull_Should_RunOneRowWhoseArgumentIsNull()
    {
      NullInputFixture.Seen = "unset";
      List<ReportedTestCase> discovered = [.. TestRunner.DiscoverTestCases(typeof(NullInputFixture))];
      discovered.Count.ShouldBe(1);
      discovered[0].DisplayName.ShouldBe("One(null)");
      IReadOnlyList<object?>? parameters = discovered[0].Parameters;
      parameters.ShouldNotBeNull();
      parameters.Count.ShouldBe(1);
      parameters[0].ShouldBeNull();

      ExitCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<NullInputFixture>(sink);
      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      sink.Results.Single().State.ShouldBe(TestNodeState.Passed);
      sink.Results.Single().DisplayName.ShouldBe("One(null)");
      NullInputFixture.Seen.ShouldBeNull();
    }

    private static async Task<int> RunQuietAsync(Func<Task<int>> run)
    {
      TextWriter previous = Terminal.Out;
      Terminal.SetOut(TextWriter.Null);
      try
      {
        return await run().ConfigureAwait(false);
      }
      finally
      {
        Terminal.SetOut(previous);
      }
    }

    private sealed class ExitCollectingSink : ITestResultSink
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

      public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) =>
        Task.CompletedTask;
    }

    private sealed class PassingExitFixture
    {
      public static Task One() => Task.CompletedTask;
    }

    private sealed class FailingExitFixture
    {
      public static Task One() => throw new InvalidOperationException("exit boom");
    }

    private sealed class SkippedExitFixture
    {
      [Skip("not this one")]
      public static Task One() => Task.CompletedTask;
    }

    private sealed class TimeoutExitFixture
    {
      [Timeout(20)]
      public static async Task One()
      {
        await Task.Delay(5000).ConfigureAwait(false);
      }
    }

    private sealed class EmptyExitFixture
    {
    }

    private sealed class NullInputFixture
    {
      public static string? Seen;

      [Input(null)]
      public static Task One(string? value)
      {
        Seen = value;
        return Task.CompletedTask;
      }
    }
  }
}
