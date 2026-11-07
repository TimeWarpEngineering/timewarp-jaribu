#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.session-fixture.lifecycle.cs
#:include test-runner.session-fixture.failure.cs
#:include test-runner.session-fixture.fixtures.cs

#region Purpose
// Tests session-scoped fixtures: RegisterSessionFixture + SessionFixture.GetAsync,
// lazy create, nesting (outer session share), dispose at session end, fail-fast
// registration validation, and teaching errors for unregistered types.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (SessionFixture_Given_)
// - Method = Given condition + Should + Result
//
// Nested fixture classes MUST NOT ModuleInitializer-register session fixtures.
// Each meta-test uses ClearRegisteredSessionFixtures in finally.
// Failure cases and fixture types live in sibling files so each file stays
// within the line cap. Single-file runs include those siblings.
// Multi-class share uses BeginTestSession + two RunTestsAsync without mutating
// global RegisteredTestClasses.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  /// <summary>
  /// Collects completed test nodes for session-fixture meta-test assertions.
  /// </summary>
  sealed class SessionFixtureCollectingSink : ITestResultSink
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

  /// <summary>
  /// Throws from OnTestCompleted so a session dispose failure can be checked for masking.
  /// </summary>
  sealed class SessionDisposeThrowingSink : ITestResultSink
  {
    public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
    public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestCompletedAsync(TestNodeInfo node)
    {
      throw new InvalidOperationException("sink boom");
    }

    public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;
    public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
  }

  [TestTag("Jaribu")]
  public partial class SessionFixture_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SessionFixture_Given_>();

    public static async Task DoubleRegister_Should_Throw()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      try
      {
        TestRunner.RegisterSessionFixture<CountingSessionFixture>();
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
          TestRunner.RegisterSessionFixture<CountingSessionFixture>);
        ex.Message.ShouldContain("already registered");
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
      }

      await Task.CompletedTask;
    }

    public static async Task MissingCreateAsync_Should_FailRegistration()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      try
      {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
          TestRunner.RegisterSessionFixture<MissingCreateAsyncFixture>);
        ex.Message.ShouldContain("CreateAsync");
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
      }

      await Task.CompletedTask;
    }

    public static async Task BadCreateAsyncSignature_Should_FailRegistration()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      try
      {
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
          TestRunner.RegisterSessionFixture<BadCreateAsyncSignatureFixture>);
        ex.Message.ShouldContain("CreateAsync");
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
      }

      await Task.CompletedTask;
    }

    public static async Task UnregisteredGetAsync_Should_ThrowTeachingError()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.BeginTestSession();
      try
      {
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
          async () => await SessionFixture.GetAsync<CountingSessionFixture>());
        ex.Message.ShouldContain("is not registered");
        ex.Message.ShouldContain("RegisterSessionFixture");
      }
      finally
      {
        await TestRunner.EndTestSessionAsync();
        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    public static async Task ClearWithLiveInstance_Should_Throw()
    {
      CountingSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      TestRunner.BeginTestSession();
      try
      {
        _ = await SessionFixture.GetAsync<CountingSessionFixture>();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
          TestRunner.ClearRegisteredSessionFixtures);
        ex.Message.ShouldContain("live instance");
      }
      finally
      {
        await TestRunner.EndTestSessionAsync();
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
      }
    }

    public static async Task Clear_Should_AllowReRegister()
    {
      TestRunner.ClearRegisteredSessionFixtures();
      try
      {
        TestRunner.RegisterSessionFixture<CountingSessionFixture>();
        TestRunner.ClearRegisteredSessionFixtures();
        // Should not throw after clear
        TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
      }

      await Task.CompletedTask;
    }
  }
}
