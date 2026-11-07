#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.timeout-policy.fixtures.cs

#region Purpose
// Proves timeout validation, timer disposal, late-fault tracing, and hook budgets.
#endregion

#region Design
// A recording TimeProvider stands in for the clock. Tests fire its timers,
// so the suite does not sleep for the configured budget. Fixtures are not
// registered. Only TimeoutPolicy_Given_ is a top-level test class.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Diagnostics;

  [TestTag("Jaribu")]
  public class TimeoutPolicy_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TimeoutPolicy_Given_>();

    public static async Task Attribute_Should_RejectNonPositiveMilliseconds()
    {
      Should.Throw<ArgumentOutOfRangeException>(() => new TimeoutAttribute(0));
      Should.Throw<ArgumentOutOfRangeException>(() => new TimeoutAttribute(-1));
      TimeoutAttribute created = new(15);
      created.Milliseconds.ShouldBe(15);
      Should.Throw<ArgumentOutOfRangeException>(() => TestRunner.DefaultHookTimeout = TimeSpan.Zero);
      Should.Throw<ArgumentOutOfRangeException>(
        () => TestRunner.DefaultHookTimeout = TimeSpan.FromMilliseconds(-1));
      Should.Throw<ArgumentOutOfRangeException>(
        () => TestRunner.DefaultHookTimeout = TimeSpan.FromTicks(1));
      await Task.CompletedTask;
    }

    public static async Task FastBody_Should_DisposeTheTimer()
    {
      using ClockScope clock = new();
      TimeoutPolicyCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<FastTimeoutFixture>(sink);

      stats.Success.ShouldBeTrue();
      clock.Provider.TimerCount.ShouldBe(1);
      clock.Provider.TimerAt(0).Disposed.ShouldBeTrue();
    }

    public static async Task NoBudget_Should_NotCreateATimer()
    {
      using ClockScope clock = new();
      TimeoutPolicyCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<NoBudgetFixture>(sink);

      stats.Success.ShouldBeTrue();
      clock.Provider.TimerCount.ShouldBe(0);
    }

    public static async Task ThrownTimeoutException_Should_StayFailed()
    {
      TimeoutPolicyCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ThrowsTimeoutFixture>(sink);

      stats.Success.ShouldBeFalse();
      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Failed);
      node.Exception.ShouldBeOfType<TimeoutException>();
      node.Exception.Message.ShouldBe("body timeout");
    }

    public static async Task LateFault_Should_BeTraced()
    {
      LateFaultFixture.Reset();
      using ClockScope clock = new();
      LineListener listener = new();
      Trace.Listeners.Add(listener);
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<LateFaultFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        sink.Results.Single().State.ShouldBe(TestNodeState.Timeout);
        LateFaultFixture.Gate.TrySetResult();
        await WaitForTraceAsync(listener, "late boom");
      }
      finally
      {
        Trace.Listeners.Remove(listener);
        LateFaultFixture.Gate.TrySetResult();
      }
    }

    public static async Task Setup_Should_TimeoutBeforeTheBody()
    {
      SetupHangFixture.Reset();
      using ClockScope clock = new();
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<SetupHangFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.TimerCount.ShouldBe(1);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        TestNodeInfo node = sink.Results.Single();
        node.State.ShouldBe(TestNodeState.Timeout);
        node.Message.ShouldNotBeNull();
        node.Message.ShouldContain("Setup");
        SetupHangFixture.Body.ShouldBe(0);
        SetupHangFixture.CleanUpCount.ShouldBe(0);
        clock.Provider.TimerAt(0).Disposed.ShouldBeTrue();
      }
      finally
      {
        SetupHangFixture.Gate.TrySetResult();
      }
    }

    public static async Task DefaultHookTimeout_Should_TimeOutCleanUp()
    {
      CleanUpHangFixture.Reset();
      using ClockScope clock = new(TimeSpan.FromMinutes(1));
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<CleanUpHangFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        TestNodeInfo node = sink.Results.Single();
        node.State.ShouldBe(TestNodeState.Timeout);
        node.Exception.ShouldBeOfType<TimeoutException>();
        node.Message.ShouldNotBeNull();
        node.Message.ShouldContain("CleanUp");
        CleanUpHangFixture.Body.ShouldBe(1);
      }
      finally
      {
        CleanUpHangFixture.Gate.TrySetResult();
      }
    }

    public static async Task SetupOnce_Should_TimeoutEveryCase()
    {
      SetupOnceHangFixture.Reset();
      using ClockScope clock = new();
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<SetupOnceHangFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        stats.FailedCount.ShouldBe(2);
        sink.Results.Count.ShouldBe(2);
        sink.Results.ShouldAllBe(node => node.State == TestNodeState.Timeout);
        SetupOnceHangFixture.Body.ShouldBe(0);
      }
      finally
      {
        SetupOnceHangFixture.Gate.TrySetResult();
      }
    }

    public static async Task CleanUpOnce_Should_EmitTimeoutNode()
    {
      CleanUpOnceHangFixture.Reset();
      using ClockScope clock = new();
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<CleanUpOnceHangFixture>(sink);
        await WaitForEnteredTimerAsync(clock.Provider, () => CleanUpOnceHangFixture.Entered > 0);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        sink.Results.Single(node => node.DisplayName == "Only").State.ShouldBe(TestNodeState.Passed);
        TestNodeInfo synthetic = sink.Results.Single(node => node.DisplayName == "CleanUpOnce");
        synthetic.State.ShouldBe(TestNodeState.Timeout);
        synthetic.Exception.ShouldBeOfType<TimeoutException>();
      }
      finally
      {
        CleanUpOnceHangFixture.Gate.TrySetResult();
      }
    }

    public static async Task AlreadyFaultedAtFire_Should_ReportTheStoredFault()
    {
      FaultAtFireFixture.Reset();
      using ClockScope clock = new();
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<FaultAtFireFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.TimerAt(0).BeforeFire = static () =>
          FaultAtFireFixture.Gate.TrySetException(new InvalidOperationException("stored fault"));
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        TestNodeInfo node = sink.Results.Single();
        node.State.ShouldBe(TestNodeState.Failed);
        node.Exception.ShouldBeOfType<InvalidOperationException>();
        node.Exception.Message.ShouldBe("stored fault");
      }
      finally
      {
        FaultAtFireFixture.Gate.TrySetResult();
      }
    }

    public static async Task CreateTimeout_Should_LetTheNextClassRetry()
    {
      RetryAfterTimeoutFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<RetryAfterTimeoutFixture>();
      using ClockScope clock = new();
      try
      {
        TestRunner.BeginTestSession();
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> firstRun = TestRunner.RunTestsAsync<TimesOutCreateFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.FireAll();
        TestRunStats first = await firstRun.WaitAsync(TimeSpan.FromSeconds(2));
        TestRunStats second = await TestRunner.RunTestsAsync<RetriesCreateFixture>(sink);

        first.Success.ShouldBeFalse();
        sink.Results.Single(node => node.DisplayName == "First").State.ShouldBe(TestNodeState.Timeout);
        second.Success.ShouldBeTrue();
        sink.Results.Single(node => node.DisplayName == "Second").State.ShouldBe(TestNodeState.Passed);
        RetryAfterTimeoutFixture.Started.ShouldBe(2);
      }
      finally
      {
        RetryAfterTimeoutFixture.Gate.TrySetResult();
        try
        {
          await TestRunner.EndTestSessionAsync();
        }
        catch (InvalidOperationException)
        {
        }

        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    public static async Task CreateAsync_Should_TimeoutAndDisposeTheLateInstance()
    {
      CreateHangFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CreateHangFixture>();
      using ClockScope clock = new();
      try
      {
        TimeoutPolicyCollectingSink sink = new();
        Task<TestRunStats> run = TestRunner.RunTestsAsync<UsesCreateHangFixture>(sink);
        await WaitForTimerAsync(clock.Provider);
        clock.Provider.FireAll();
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

        stats.Success.ShouldBeFalse();
        TestNodeInfo node = sink.Results.Single(result => result.DisplayName == "Only");
        node.State.ShouldBe(TestNodeState.Timeout);
        node.Exception.ShouldBeOfType<TimeoutException>();
        node.Message.ShouldNotBeNull();
        node.Message.ShouldContain("CreateAsync");
        CreateHangFixture.Gate.TrySetResult();
        await CreateHangFixture.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
      }
      finally
      {
        CreateHangFixture.Gate.TrySetResult();
        try
        {
          await TestRunner.EndTestSessionAsync();
        }
        catch (InvalidOperationException)
        {
        }

        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    private static async Task WaitForTimerAsync(RecordingTimeProvider provider)
      => await WaitForEnteredTimerAsync(provider, static () => true);

    /// <summary>
    /// A completed hook can leave a short-lived timer. Wait until the hanging hook
    /// has entered and its timer is still alive.
    /// </summary>
    private static async Task WaitForEnteredTimerAsync(
      RecordingTimeProvider provider,
      Func<bool> entered)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
      while (!entered() || !provider.HasLiveTimer())
      {
        if (DateTimeOffset.UtcNow > deadline)
        {
          throw new TimeoutException("The runner did not create a timeout timer.");
        }

        await Task.Delay(10);
      }
    }

    private static async Task WaitForTraceAsync(LineListener listener, string text)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
      while (!listener.Text.Contains(text, StringComparison.Ordinal))
      {
        if (DateTimeOffset.UtcNow > deadline)
        {
          throw new TimeoutException($"Trace did not contain '{text}'.");
        }

        await Task.Delay(10);
      }
    }
  }

}
