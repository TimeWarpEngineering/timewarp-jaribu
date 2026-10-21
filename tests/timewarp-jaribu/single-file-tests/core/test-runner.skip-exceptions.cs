#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests [Skip] attribute behavior and exception handling including runtime exceptions,
// wrapped TargetInvocationExceptions, and async exceptions after await.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (SkipExceptions_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.SkipExceptions_Given_.SkipAttribute_Should_ReportReason
//
// SkipExceptionsFixture is not registered. SkipExceptions_Given_ drives it with RunTestsAsync.
// A synchronous Task method proves TargetInvocationException is unwrapped to the inner exception.
// An async method's awaited exception is recorded as Error, which still fails the run.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  sealed class SkipExceptionsCollectingSink : ITestResultSink
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
  public class SkipExceptions_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SkipExceptions_Given_>();

    public static async Task SkipAttribute_Should_ReportReason()
    {
      SkipExceptionsFixture.Reset();
      SkipExceptionsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<SkipExceptionsFixture>(sink);

      stats.SkippedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBe(3);
      stats.Success.ShouldBeFalse();
      SkipExceptionsFixture.SkipBodyRuns.ShouldBe(0);

      TestNodeInfo skipped = sink.Results.Single(node =>
        node.DisplayName == nameof(SkipExceptionsFixture.SkipAttribute_Should_ReportReason));
      skipped.State.ShouldBe(TestNodeState.Skipped);
      skipped.Message.ShouldBe("WIP - Work in progress");
      skipped.Exception.ShouldBeNull();
    }

    public static async Task RuntimeException_Should_FailWithArgumentException()
    {
      SkipExceptionsCollectingSink sink = new();
      await TestRunner.RunTestsAsync<SkipExceptionsFixture>(sink);

      TestNodeInfo node = sink.Results.Single(result =>
        result.DisplayName == nameof(SkipExceptionsFixture.RuntimeException_Should_Fail));
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<ArgumentException>();
      node.Exception.Message.ShouldContain("Intentional runtime exception");
    }

    public static async Task InvocationException_Should_UnwrapAndFail()
    {
      SkipExceptionsCollectingSink sink = new();
      await TestRunner.RunTestsAsync<SkipExceptionsFixture>(sink);

      TestNodeInfo node = sink.Results.Single(result =>
        result.DisplayName == nameof(SkipExceptionsFixture.InvocationException_Should_UnwrapAndFail));
      node.State.ShouldBe(TestNodeState.Failed);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<InvalidOperationException>();
      node.Exception.Message.ShouldContain("Inner exception for SKIP-03");
    }

    public static async Task AsyncException_Should_FailAfterAwait()
    {
      SkipExceptionsCollectingSink sink = new();
      await TestRunner.RunTestsAsync<SkipExceptionsFixture>(sink);

      TestNodeInfo node = sink.Results.Single(result =>
        result.DisplayName == nameof(SkipExceptionsFixture.AsyncException_Should_FailAfterAwait));
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldNotBeNull();
      node.Exception.ShouldBeOfType<NotSupportedException>();
      node.Exception.Message.ShouldContain("Async exception after await");
    }

    public static async Task PassingMethod_Should_NotBeAffectedBySkips()
    {
      SkipExceptionsFixture.Reset();
      SkipExceptionsCollectingSink sink = new();
      await TestRunner.RunTestsAsync<SkipExceptionsFixture>(sink);

      TestNodeInfo node = sink.Results.Single(result =>
        result.DisplayName == nameof(SkipExceptionsFixture.PassingMethod_Should_NotBeAffectedBySkips));
      node.State.ShouldBe(TestNodeState.Passed);
      SkipExceptionsFixture.PassingRuns.ShouldBe(1);
    }
  }

  public class SkipExceptionsFixture
  {
    public static int SkipBodyRuns;
    public static int PassingRuns;

    public static void Reset()
    {
      SkipBodyRuns = 0;
      PassingRuns = 0;
    }

    [Skip("WIP - Work in progress")]
    public static async Task SkipAttribute_Should_ReportReason()
    {
      SkipBodyRuns++;
      await Task.CompletedTask;
    }

    public static async Task RuntimeException_Should_Fail()
    {
      await Task.CompletedTask;
      throw new ArgumentException("Intentional runtime exception for SKIP-02");
    }

    public static Task InvocationException_Should_UnwrapAndFail()
    {
      throw new InvalidOperationException("Inner exception for SKIP-03");
    }

    public static async Task AsyncException_Should_FailAfterAwait()
    {
      await Task.Delay(1);
      throw new NotSupportedException("Async exception after await for SKIP-04");
    }

    public static async Task PassingMethod_Should_NotBeAffectedBySkips()
    {
      PassingRuns++;
      await Task.CompletedTask;
    }
  }
}
