#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.cancellation.fixtures.cs

#region Purpose
// Proves a caller token cancels the in-flight test, skips the rest, and fails the run.
#endregion

#region Design
// Fixtures block on a TaskCompletionSource instead of a long delay. The meta-test
// releases that source after the assertions so a missed cancel fails in five seconds
// and the abandoned task still completes. Fixtures are not ModuleInitializer-registered.
// Scenario fixtures live in a sibling file so each file stays within the line cap.
// A single-file run includes that sibling. One fixture throws from CleanUpOnce
// before it returns a task, after the body has cancelled the run.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;

  [TestTag("Jaribu")]
  public class Cancellation_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Cancellation_Given_>();

    public static async Task MidRun_Should_CancelTheCurrentTestAndSkipTheRest()
    {
      CancellationCollectingSink sink = new();
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      try
      {
        Task<TestRunStats> run = TestRunner.RunTestsAsync<HoldPairFixture>(
          sink,
          cancellationToken: cts.Token);
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));

        stats.Success.ShouldBeFalse();
        stats.FailedCount.ShouldBe(1);
        HoldPairFixture.Entered.ShouldBe(1);
        HoldPairFixture.Finished.ShouldBe(0);
        TestNodeInfo node = sink.Results.Single();
        node.State.ShouldBe(TestNodeState.Cancelled);
        node.Exception.ShouldBeOfType<OperationCanceledException>();
        node.Message.ShouldBe("Cancelled");
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task MidRun_Should_ExitNonZero()
    {
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      try
      {
        Task<int> run = TestRunner.RunTests<HoldPairFixture>(cancellationToken: cts.Token);
        int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));
        exitCode.ShouldBe(1);
        HoldPairFixture.Entered.ShouldBe(1);
        HoldPairFixture.Finished.ShouldBe(0);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task AlreadyCancelled_Should_StartNothingAndExitNonZero()
    {
      using CancellationTokenSource cts = new();
      cts.Cancel();
      QuickFixture.Entered = 0;

      int exitCode = await TestRunner.RunTests<QuickFixture>(cancellationToken: cts.Token);

      exitCode.ShouldBe(1);
      QuickFixture.Entered.ShouldBe(0);
    }

    public static async Task Timeout_Should_StayTimeoutWhenTheTokenIsNotCancelled()
    {
      CancellationCollectingSink sink = new();
      TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TimeoutHoldFixture.Release = release;
      try
      {
        Task<TestRunStats> run = TestRunner.RunTestsAsync<TimeoutHoldFixture>(sink);
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));

        stats.Success.ShouldBeFalse();
        sink.Results.Single().State.ShouldBe(TestNodeState.Timeout);
      }
      finally
      {
        release.TrySetResult();
      }
    }

    public static async Task Cancel_Should_WinOverTimeout()
    {
      CancellationCollectingSink sink = new();
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      CancelBeatsTimeoutFixture.Cancellation = cts;
      CancelBeatsTimeoutFixture.Release = release;
      CancelBeatsTimeoutFixture.Entered = 0;
      try
      {
        Task<TestRunStats> run = TestRunner.RunTestsAsync<CancelBeatsTimeoutFixture>(
          sink,
          cancellationToken: cts.Token);
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));

        stats.Success.ShouldBeFalse();
        sink.Results.Single().State.ShouldBe(TestNodeState.Cancelled);
        CancelBeatsTimeoutFixture.Entered.ShouldBe(1);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task Setup_Should_StopBeforeTheBody()
    {
      CancellationCollectingSink sink = new();
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      SetupHoldFixture.Bind(cts, release);
      try
      {
        Task<TestRunStats> run = TestRunner.RunTestsAsync<SetupHoldFixture>(
          sink,
          cancellationToken: cts.Token);
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));

        stats.Success.ShouldBeFalse();
        sink.Results.Single().State.ShouldBe(TestNodeState.Cancelled);
        SetupHoldFixture.Body.ShouldBe(0);
        SetupHoldFixture.SetupFinished.ShouldBe(0);
        SetupHoldFixture.CleanUpEntered.ShouldBe(0);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task SetupOnce_Should_SkipBodiesAndStillCleanUpOnce()
    {
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      SetupOnceHoldFixture.Bind(cts, release);
      try
      {
        Task<int> run = TestRunner.RunTests<SetupOnceHoldFixture>(cancellationToken: cts.Token);
        int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));

        exitCode.ShouldBe(1);
        SetupOnceHoldFixture.Body.ShouldBe(0);
        SetupOnceHoldFixture.CleanUpOnceEntered.ShouldBe(1);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task CleanUp_Should_CancelAPassingBody()
    {
      CancellationCollectingSink sink = new();
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      CleanUpHoldFixture.Bind(cts, release);
      try
      {
        Task<TestRunStats> run = TestRunner.RunTestsAsync<CleanUpHoldFixture>(
          sink,
          cancellationToken: cts.Token);
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));

        stats.Success.ShouldBeFalse();
        sink.Results.Single().State.ShouldBe(TestNodeState.Cancelled);
        CleanUpHoldFixture.Body.ShouldBe(1);
        CleanUpHoldFixture.CleanUpFinished.ShouldBe(0);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
      }
    }

    public static async Task CleanUpOnce_Should_ReturnOneWhenItThrowsBeforeATaskAfterCancel()
    {
      using CancellationTokenSource cts = new();
      SyncThrowCleanUpOnceFixture.Bind(cts);

      Task<int> run = TestRunner.RunTests<SyncThrowCleanUpOnceFixture>(cancellationToken: cts.Token);
      int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));

      exitCode.ShouldBe(1);
      SyncThrowCleanUpOnceFixture.Body.ShouldBe(1);
      SyncThrowCleanUpOnceFixture.CleanUpOnceEntered.ShouldBe(1);
    }

    public static async Task RunAllTests_Should_NotStartTheNextClass()
    {
      Type[] saved = [.. TestRunner.RegisteredTestClasses];
      (CancellationTokenSource cts, TaskCompletionSource release) = Hold.Reset();
      LaterClassFixture.Entered = 0;
      try
      {
        TestRunner.ClearRegisteredTests();
        TestRunner.RegisterTests<HoldPairFixture>();
        TestRunner.RegisterTests<LaterClassFixture>();

        Task<int> run = TestRunner.RunAllTests(cancellationToken: cts.Token);
        int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(5));

        exitCode.ShouldBe(1);
        HoldPairFixture.Entered.ShouldBe(1);
        HoldPairFixture.Finished.ShouldBe(0);
        LaterClassFixture.Entered.ShouldBe(0);
      }
      finally
      {
        release.TrySetResult();
        cts.Dispose();
        TestRunner.ClearRegisteredTests();
        MethodInfo register = typeof(TestRunner).GetMethod(nameof(TestRunner.RegisterTests))!;
        foreach (Type type in saved)
        {
          register.MakeGenericMethod(type).Invoke(null, null);
        }
      }
    }

    public static async Task CreateAsync_Should_StopWhenTheTokenFires()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<SlowCreateFixture>();
      using CancellationTokenSource cts = new();
      TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
      SlowCreateFixture.Reset(started, release);
      TestRunner.BeginTestSession();
      try
      {
        Task<SlowCreateFixture> get = SessionFixture.GetAsync<SlowCreateFixture>(cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await get);
        SlowCreateFixture.Finished.ShouldBe(0);
        SlowCreateFixture.Entered.ShouldBe(1);
      }
      finally
      {
        release.TrySetResult();
        await TestRunner.EndTestSessionAsync();
        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    public static async Task DisposeAsync_Should_StopWhenTheTokenFires()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<SlowDisposeFixture>();
      using CancellationTokenSource cts = new();
      TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
      SlowDisposeFixture.Reset(started, release);
      TestRunner.BeginTestSession();
      try
      {
        await SessionFixture.GetAsync<SlowDisposeFixture>();
        Task end = TestRunner.EndTestSessionAsync(cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await end);
        SlowDisposeFixture.Finished.ShouldBe(0);
        SlowDisposeFixture.Entered.ShouldBe(1);
      }
      finally
      {
        release.TrySetResult();
        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    public static async Task EndSession_Should_StartDisposeOnEveryFixtureWhenAlreadyCancelled()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<FirstCancelledDisposeFixture>();
      TestRunner.RegisterSessionFixture<SecondCancelledDisposeFixture>();
      using CancellationTokenSource cts = new();
      cts.Cancel();
      TaskCompletionSource firstRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TaskCompletionSource secondRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TaskCompletionSource firstFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
      TaskCompletionSource secondFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
      FirstCancelledDisposeFixture.Reset(firstRelease, firstFinished);
      SecondCancelledDisposeFixture.Reset(secondRelease, secondFinished);
      TestRunner.BeginTestSession();
      try
      {
        await SessionFixture.GetAsync<FirstCancelledDisposeFixture>();
        await SessionFixture.GetAsync<SecondCancelledDisposeFixture>();
        await Should.ThrowAsync<OperationCanceledException>(
          async () => await TestRunner.EndTestSessionAsync(cts.Token));
        FirstCancelledDisposeFixture.Entered.ShouldBe(1);
        SecondCancelledDisposeFixture.Entered.ShouldBe(1);
      }
      finally
      {
        firstRelease.TrySetResult();
        secondRelease.TrySetResult();
        try
        {
          await Task.WhenAll(firstFinished.Task, secondFinished.Task)
            .WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
          // Dispose did not finish. The session end below still runs when it is open.
        }

        await TestRunner.EndTestSessionAsync();
        TestRunner.ClearRegisteredSessionFixtures();
      }
    }
  }

  file sealed class CancellationCollectingSink : ITestResultSink
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

  file static class Hold
  {
    public static (CancellationTokenSource Cts, TaskCompletionSource Release) Reset()
    {
      CancellationTokenSource cts = new();
      TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
      HoldPairFixture.Cancellation = cts;
      HoldPairFixture.Release = release;
      HoldPairFixture.Entered = 0;
      HoldPairFixture.Finished = 0;
      return (cts, release);
    }
  }
}
