#region Purpose
// Session-fixture create and dispose failures, including session-of-one masking.
#endregion

namespace TestRunner_;

public partial class SessionFixture_Given_
{
  public static async Task CreateFailure_Should_SurfaceAsTestFailure()
  {
    FailingCreateSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingCreateSessionFixture>();
    try
    {
      SessionFixtureCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<UsesFailingCreateFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBeGreaterThanOrEqualTo(1);
      FailingCreateSessionFixture.CreateCount.ShouldBe(1);
      sink.Results.Any(r =>
        r.State == TestNodeState.Error &&
        r.Exception?.Message.Contains("create boom", StringComparison.Ordinal) == true).ShouldBeTrue();
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
      FailingCreateSessionFixture.Reset();
    }
  }

  /// <summary>
  /// Failed CreateAsync must not be retried on a later GetAsync in the same session.
  /// </summary>
  public static async Task CreateFailure_Should_BeStickyAcrossClassesInSession()
  {
    FailingCreateSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingCreateSessionFixture>();
    try
    {
      SessionFixtureCollectingSink sink = new();
      TestRunner.BeginTestSession();
      try
      {
        TestRunStats first = await TestRunner.RunTestsAsync<UsesFailingCreateFixture>(sink);
        TestRunStats second = await TestRunner.RunTestsAsync<UsesFailingCreateFixtureB>(sink);

        first.Success.ShouldBeFalse();
        second.Success.ShouldBeFalse();
        FailingCreateSessionFixture.CreateCount.ShouldBe(1);
      }
      finally
      {
        await TestRunner.EndTestSessionAsync();
      }
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
      FailingCreateSessionFixture.Reset();
    }
  }

  public static async Task DisposeFailure_Should_FailEndSession()
  {
    FailingDisposeSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingDisposeSessionFixture>();
    try
    {
      SessionFixtureCollectingSink sink = new();
      InvalidOperationException? disposeEx = null;

      // Outer session so dispose happens on EndTestSessionAsync (not swallowed by
      // RunTestsAsyncCore when reporting test success).
      TestRunner.BeginTestSession();
      try
      {
        TestRunStats stats = await TestRunner.RunTestsAsync<UsesFailingDisposeFixture>(sink);
        stats.Success.ShouldBeTrue();
        FailingDisposeSessionFixture.CreateCount.ShouldBe(1);
        FailingDisposeSessionFixture.DisposeCount.ShouldBe(0);
      }
      finally
      {
        try
        {
          await TestRunner.EndTestSessionAsync();
        }
        catch (InvalidOperationException ex)
        {
          disposeEx = ex;
        }
      }

      disposeEx.ShouldNotBeNull();
      disposeEx.Message.ShouldContain("DisposeAsync");
      FailingDisposeSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
      FailingDisposeSessionFixture.Reset();
    }
  }

  /// <summary>
  /// A CreateAsync completing after the session fully ended must not cache the
  /// instance for a later session — the orphan is disposed and GetAsync throws.
  /// </summary>
  public static async Task SessionEndDuringCreate_Should_DisposeOrphanAndThrow()
  {
    BlockedCreateSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<BlockedCreateSessionFixture>();
    try
    {
      TestRunner.BeginTestSession();
      Task<BlockedCreateSessionFixture> getTask = SessionFixture.GetAsync<BlockedCreateSessionFixture>();
      await BlockedCreateSessionFixture.CreateStarted.Task;

      await TestRunner.EndTestSessionAsync();
      BlockedCreateSessionFixture.ReleaseCreate.SetResult();

      InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () => await getTask);
      ex.Message.ShouldContain("ended while");
      BlockedCreateSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
      BlockedCreateSessionFixture.Reset();
    }
  }

  public static async Task SessionEndDuringCreate_Should_ReportErrorFromTheBody()
  {
    BlockedCreateSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<BlockedCreateSessionFixture>();
    try
    {
      TestRunner.BeginTestSession();
      SessionFixtureCollectingSink sink = new();
      Task<TestRunStats> run = TestRunner.RunTestsAsync<UsesBlockedCreateFromBody>(sink);
      await BlockedCreateSessionFixture.CreateStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
      await TestRunner.EndTestSessionAsync();
      BlockedCreateSessionFixture.ReleaseCreate.TrySetResult();
      TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(2));

      stats.Success.ShouldBeFalse();
      TestNodeInfo node = sink.Results.Single(result => result.DisplayName == "OnlyTest");
      node.State.ShouldBe(TestNodeState.Error);
      node.Exception.ShouldBeOfType<InvalidOperationException>();
      node.Exception.Message.ShouldContain("ended while");
      BlockedCreateSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      BlockedCreateSessionFixture.ReleaseCreate.TrySetResult();
      await EndLeftoverSessionAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      BlockedCreateSessionFixture.Reset();
    }
  }

  public static async Task SessionOfOneDisposeFailure_Should_FailRunWithoutThrowing()
  {
    FailingDisposeSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingDisposeSessionFixture>();
    try
    {
      SessionFixtureCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<UsesFailingDisposeFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBeGreaterThanOrEqualTo(1);
      FailingDisposeSessionFixture.DisposeCount.ShouldBe(1);

      TestNodeInfo body = sink.Results.Single(node => node.DisplayName == "OnlyTest");
      body.State.ShouldBe(TestNodeState.Passed);

      TestNodeInfo disposeNode = sink.Results.Single(node => node.DisplayName == "SessionFixtureDispose");
      disposeNode.State.ShouldBe(TestNodeState.Failed);
      disposeNode.Message.ShouldNotBeNull();
      disposeNode.Message.ShouldContain("dispose boom");
    }
    finally
    {
      await EndLeftoverSessionAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      FailingDisposeSessionFixture.Reset();
    }
  }

  public static async Task SessionOfOneDisposeFailure_Should_ReturnExitCodeOne()
  {
    FailingDisposeSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingDisposeSessionFixture>();
    try
    {
      int exitCode = await TestRunner.RunTests<UsesFailingDisposeFixture>();
      exitCode.ShouldBe(1);
      FailingDisposeSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      await EndLeftoverSessionAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      FailingDisposeSessionFixture.Reset();
    }
  }

  public static async Task SessionOfOneDisposeFailure_Should_KeepBodyException()
  {
    FailingDisposeSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingDisposeSessionFixture>();
    try
    {
      SessionFixtureCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<UsesFailingDisposeAndFailingBody>(sink);

      stats.Success.ShouldBeFalse();
      TestNodeInfo body = sink.Results.Single(node => node.DisplayName == "OnlyTest");
      body.State.ShouldBe(TestNodeState.Failed);
      body.Exception.ShouldBeOfType<ArgumentException>();
      body.Exception.Message.ShouldBe("body boom");
      sink.Results.Any(node => node.DisplayName == "SessionFixtureDispose").ShouldBeTrue();
      FailingDisposeSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      await EndLeftoverSessionAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      FailingDisposeSessionFixture.Reset();
    }
  }

  public static async Task SessionOfOneDisposeFailure_Should_NotMaskInFlightException()
  {
    FailingDisposeSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FailingDisposeSessionFixture>();
    try
    {
      InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
        () => TestRunner.RunTestsAsync<UsesFailingDisposeFixture>(new SessionDisposeThrowingSink()));
      ex.Message.ShouldContain("sink boom");
      FailingDisposeSessionFixture.DisposeCount.ShouldBe(1);
    }
    finally
    {
      await EndLeftoverSessionAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      FailingDisposeSessionFixture.Reset();
    }
  }

  private static async Task EndLeftoverSessionAsync()
  {
    try
    {
      await TestRunner.EndTestSessionAsync();
    }
    catch (InvalidOperationException)
    {
      // The session already ended, or dispose failed after the instance was cleared.
    }
  }
}
