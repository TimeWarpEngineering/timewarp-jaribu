#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Proves a throwing per-test CleanUp keeps the body outcome (fail or timeout)
// and still surfaces the cleanup failure.
#endregion

#region Design
// Nested fixtures are driven by RunTestsAsync. Only CleanupOutcome_Given_ registers,
// so the failing fixtures are not top-level CI tests.
// A passing body becomes Error, and its message starts with CleanUp.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  sealed class CleanupOutcomeCollectingSink : ITestResultSink
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
  public class CleanupOutcome_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<CleanupOutcome_Given_>();

    public static async Task BodyFailure_Should_KeepBodyExceptionWhenCleanUpThrows()
    {
      CleanupOutcomeCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanupOutcomeBodyFailFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);

      TestNodeInfo node = sink.Results.Single();
      // Awaited async exceptions are Error. CleanUp must not replace that state or exception.
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldBeOfType<ArgumentException>();
      node.Exception.Message.ShouldBe("body boom");
      node.Message.ShouldNotBeNull();
      node.Message.ShouldContain("body boom");
      node.Message.ShouldContain("cleanup boom");
    }

    public static async Task Timeout_Should_StayTimeoutWhenCleanUpThrows()
    {
      CleanupOutcomeTimeoutFixture.CleanUpCount = 0;
      CleanupOutcomeCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanupOutcomeTimeoutFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      CleanupOutcomeTimeoutFixture.CleanUpCount.ShouldBe(1);

      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Timeout);
      node.Exception.ShouldBeOfType<TimeoutException>();
      node.Message.ShouldNotBeNull();
      node.Message.ShouldContain("Timeout");
      node.Message.ShouldContain("cleanup boom");
    }

    public static async Task SyncBodyFailure_Should_StayFailedWhenCleanUpThrows()
    {
      CleanupOutcomeCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanupOutcomeSyncFailFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);

      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Failed);
      node.Exception.ShouldBeOfType<ArgumentException>();
      node.Exception.Message.ShouldBe("body boom");
      node.Message.ShouldNotBeNull();
      node.Message.ShouldContain("cleanup boom");
    }

    public static async Task PassingBody_Should_BecomeErrorWhenCleanUpThrows()
    {
      CleanupOutcomeCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanupOutcomePassFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);

      TestNodeInfo node = sink.Results.Single();
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldBeOfType<InvalidOperationException>();
      node.Exception.Message.ShouldBe("cleanup boom");
      node.Message.ShouldNotBeNull();
      node.Message.ShouldStartWith("CleanUp ");
      node.Message.ShouldContain("cleanup boom");
    }
  }

  public class CleanupOutcomeBodyFailFixture
  {
    public static async Task CleanUp()
    {
      throw new InvalidOperationException("cleanup boom");
    }

    public static async Task OnlyTest()
    {
      throw new ArgumentException("body boom");
    }
  }

  public class CleanupOutcomeTimeoutFixture
  {
    public static int CleanUpCount;

    public static async Task CleanUp()
    {
      CleanUpCount++;
      throw new InvalidOperationException("cleanup boom");
    }

    [Timeout(100)]
    public static async Task OnlyTest()
    {
      await Task.Delay(TimeSpan.FromSeconds(2));
    }
  }

  public class CleanupOutcomeSyncFailFixture
  {
    public static async Task CleanUp()
    {
      throw new InvalidOperationException("cleanup boom");
    }

    public static Task OnlyTest()
    {
      throw new ArgumentException("body boom");
    }
  }

  public class CleanupOutcomePassFixture
  {
    public static async Task CleanUp()
    {
      throw new InvalidOperationException("cleanup boom");
    }

    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }
}
