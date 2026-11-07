#region Purpose
// Session-fixture create, share, and skip behavior across one class or a host session.
#endregion

namespace TestRunner_;

public partial class SessionFixture_Given_
{
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
