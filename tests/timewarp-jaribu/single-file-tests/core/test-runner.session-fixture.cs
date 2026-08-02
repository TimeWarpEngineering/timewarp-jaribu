#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

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

  [TestTag("Jaribu")]
  public class SessionFixture_Given_
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
          SessionFixture.GetAsync<CountingSessionFixture>);
        ex.Message.ShouldContain("is not registered");
        ex.Message.ShouldContain("RegisterSessionFixture");
      }
      finally
      {
        await TestRunner.EndTestSessionAsync();
        TestRunner.ClearRegisteredSessionFixtures();
      }
    }

    public static async Task SingleClass_Should_CreateOnceAndDispose()
    {
      CountingSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync<UsesSessionFixtureA>(sink);

        stats.Success.ShouldBeTrue();
        stats.PassedCount.ShouldBe(2);
        CountingSessionFixture.CreateCount.ShouldBe(1);
        CountingSessionFixture.DisposeCount.ShouldBe(1);
        CountingSessionFixture.BodyUses.ShouldBe(2);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
      }
    }

    public static async Task MultiClassSharedSession_Should_CreateOnce()
    {
      CountingSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sinkA = new();
        SessionFixtureCollectingSink sinkB = new();

        TestRunner.BeginTestSession();
        try
        {
          TestRunStats statsA = await TestRunner.RunTestsAsync<UsesSessionFixtureA>(sinkA);
          TestRunStats statsB = await TestRunner.RunTestsAsync<UsesSessionFixtureB>(sinkB);
          statsA.Success.ShouldBeTrue();
          statsB.Success.ShouldBeTrue();
          CountingSessionFixture.CreateCount.ShouldBe(1);
          CountingSessionFixture.DisposeCount.ShouldBe(0);
        }
        finally
        {
          await TestRunner.EndTestSessionAsync();
        }

        CountingSessionFixture.CreateCount.ShouldBe(1);
        CountingSessionFixture.DisposeCount.ShouldBe(1);
        CountingSessionFixture.BodyUses.ShouldBe(3);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
      }
    }

    public static async Task SeparateRunTestsAsync_Should_CreatePerCall()
    {
      CountingSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sink1 = new();
        SessionFixtureCollectingSink sink2 = new();

        TestRunStats stats1 = await TestRunner.RunTestsAsync<UsesSessionFixtureA>(sink1);
        TestRunStats stats2 = await TestRunner.RunTestsAsync<UsesSessionFixtureA>(sink2);

        stats1.Success.ShouldBeTrue();
        stats2.Success.ShouldBeTrue();
        CountingSessionFixture.CreateCount.ShouldBe(2);
        CountingSessionFixture.DisposeCount.ShouldBe(2);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
      }
    }

    public static async Task NeverGetAsync_Should_NotCreate()
    {
      CountingSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync<NeverGetsSessionFixture>(sink);

        stats.Success.ShouldBeTrue();
        stats.PassedCount.ShouldBe(1);
        CountingSessionFixture.CreateCount.ShouldBe(0);
        CountingSessionFixture.DisposeCount.ShouldBe(0);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
      }
    }

    public static async Task AllSkip_Should_NotCreate()
    {
      CountingSessionFixture.Reset();
      AllSkipSessionFixtureUser.SetupOnceCount = 0;
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync<AllSkipSessionFixtureUser>(sink);

        stats.Success.ShouldBeTrue();
        stats.SkippedCount.ShouldBe(2);
        CountingSessionFixture.CreateCount.ShouldBe(0);
        CountingSessionFixture.DisposeCount.ShouldBe(0);
        AllSkipSessionFixtureUser.SetupOnceCount.ShouldBe(0);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
        AllSkipSessionFixtureUser.SetupOnceCount = 0;
      }
    }

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
          r.State == TestNodeState.Failed &&
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

    public static async Task SecondType_Should_BeIndependent()
    {
      CountingSessionFixture.Reset();
      OtherSessionFixture.Reset();
      TestRunner.ClearRegisteredSessionFixtures();
      TestRunner.RegisterSessionFixture<CountingSessionFixture>();
      TestRunner.RegisterSessionFixture<OtherSessionFixture>();
      try
      {
        SessionFixtureCollectingSink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync<UsesTwoSessionFixtures>(sink);

        stats.Success.ShouldBeTrue();
        CountingSessionFixture.CreateCount.ShouldBe(1);
        OtherSessionFixture.CreateCount.ShouldBe(1);
        CountingSessionFixture.DisposeCount.ShouldBe(1);
        OtherSessionFixture.DisposeCount.ShouldBe(1);
      }
      finally
      {
        TestRunner.ClearRegisteredSessionFixtures();
        CountingSessionFixture.Reset();
        OtherSessionFixture.Reset();
      }
    }
  }

  // --- Session fixture types (not test classes) ---

  public sealed class CountingSessionFixture : IAsyncDisposable
  {
    public static int CreateCount;
    public static int DisposeCount;
    public static int BodyUses;

    public static void Reset()
    {
      CreateCount = 0;
      DisposeCount = 0;
      BodyUses = 0;
    }

    public static Task<CountingSessionFixture> CreateAsync()
    {
      CreateCount++;
      return Task.FromResult(new CountingSessionFixture());
    }

    public ValueTask DisposeAsync()
    {
      DisposeCount++;
      return ValueTask.CompletedTask;
    }
  }

  public sealed class OtherSessionFixture : IAsyncDisposable
  {
    public static int CreateCount;
    public static int DisposeCount;

    public static void Reset()
    {
      CreateCount = 0;
      DisposeCount = 0;
    }

    public static Task<OtherSessionFixture> CreateAsync()
    {
      CreateCount++;
      return Task.FromResult(new OtherSessionFixture());
    }

    public ValueTask DisposeAsync()
    {
      DisposeCount++;
      return ValueTask.CompletedTask;
    }
  }

  public sealed class MissingCreateAsyncFixture : IAsyncDisposable
  {
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  public sealed class BadCreateAsyncSignatureFixture : IAsyncDisposable
  {
    // Wrong: void, not Task<T> — name must stay CreateAsync for registration scan.
#pragma warning disable RCS1047 // Intentional non-async CreateAsync for negative registration test
    public static void CreateAsync()
    {
    }
#pragma warning restore RCS1047

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  public sealed class FailingCreateSessionFixture : IAsyncDisposable
  {
    public static int CreateCount;

    public static void Reset()
    {
      CreateCount = 0;
    }

    public static Task<FailingCreateSessionFixture> CreateAsync()
    {
      CreateCount++;
      throw new InvalidOperationException("create boom");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  public sealed class FailingDisposeSessionFixture : IAsyncDisposable
  {
    public static int CreateCount;
    public static int DisposeCount;

    public static void Reset()
    {
      CreateCount = 0;
      DisposeCount = 0;
    }

    public static Task<FailingDisposeSessionFixture> CreateAsync()
    {
      CreateCount++;
      return Task.FromResult(new FailingDisposeSessionFixture());
    }

    public ValueTask DisposeAsync()
    {
      DisposeCount++;
      throw new InvalidOperationException("dispose boom");
    }
  }

  // --- Nested test classes (not ModuleInitializer-registered) ---

  public class UsesSessionFixtureA
  {
    private static CountingSessionFixture? Shared;

    public static async Task SetupOnce()
    {
      Shared = await SessionFixture.GetAsync<CountingSessionFixture>();
    }

    public static async Task CleanUpOnce()
    {
      Shared = null;
      await Task.CompletedTask;
    }

    public static async Task TestOne()
    {
      Shared.ShouldNotBeNull();
      CountingSessionFixture.BodyUses++;
      await Task.CompletedTask;
    }

    public static async Task TestTwo()
    {
      Shared.ShouldNotBeNull();
      CountingSessionFixture.BodyUses++;
      await Task.CompletedTask;
    }
  }

  public class UsesSessionFixtureB
  {
    private static CountingSessionFixture? Shared;

    public static async Task SetupOnce()
    {
      Shared = await SessionFixture.GetAsync<CountingSessionFixture>();
    }

    public static async Task CleanUpOnce()
    {
      Shared = null;
      await Task.CompletedTask;
    }

    public static async Task OnlyTest()
    {
      Shared.ShouldNotBeNull();
      CountingSessionFixture.BodyUses++;
      await Task.CompletedTask;
    }
  }

  public class NeverGetsSessionFixture
  {
    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }

  public class AllSkipSessionFixtureUser
  {
    public static int SetupOnceCount;

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      _ = await SessionFixture.GetAsync<CountingSessionFixture>();
    }

    [Skip("lazy session create check")]
    public static async Task SkippedA()
    {
      await Task.CompletedTask;
    }

    [Skip("lazy session create check")]
    public static async Task SkippedB()
    {
      await Task.CompletedTask;
    }
  }

  public class UsesFailingCreateFixture
  {
    public static async Task SetupOnce()
    {
      _ = await SessionFixture.GetAsync<FailingCreateSessionFixture>();
    }

    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }

  public class UsesFailingCreateFixtureB
  {
    public static async Task SetupOnce()
    {
      _ = await SessionFixture.GetAsync<FailingCreateSessionFixture>();
    }

    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }

  public class UsesFailingDisposeFixture
  {
    private static FailingDisposeSessionFixture? Shared;

    public static async Task SetupOnce()
    {
      Shared = await SessionFixture.GetAsync<FailingDisposeSessionFixture>();
    }

    public static async Task CleanUpOnce()
    {
      Shared = null;
      await Task.CompletedTask;
    }

    public static async Task OnlyTest()
    {
      Shared.ShouldNotBeNull();
      await Task.CompletedTask;
    }
  }

  public class UsesTwoSessionFixtures
  {
    public static async Task SetupOnce()
    {
      _ = await SessionFixture.GetAsync<CountingSessionFixture>();
      _ = await SessionFixture.GetAsync<OtherSessionFixture>();
    }

    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }
}
