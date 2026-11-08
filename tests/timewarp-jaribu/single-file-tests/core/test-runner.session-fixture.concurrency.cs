#region Purpose
// Session-fixture dispose order, unbalanced host end, and overlapping begin/end.
#endregion

namespace TestRunner_;

public partial class SessionFixture_Given_
{
  public static async Task CreatedOrder_Should_DisposeNewestFirst()
  {
    DisposeOrderLog.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<FirstOrderFixture>();
    TestRunner.RegisterSessionFixture<SecondOrderFixture>();
    TestRunner.RegisterSessionFixture<ThirdOrderFixture>();
    TestRunner.BeginTestSession();
    try
    {
      _ = await SessionFixture.GetAsync<ThirdOrderFixture>();
      _ = await SessionFixture.GetAsync<FirstOrderFixture>();
      _ = await SessionFixture.GetAsync<SecondOrderFixture>();
      await TestRunner.EndTestSessionAsync();

      string[] events = DisposeOrderLog.Snapshot();
      events.ShouldBe(["dispose-second", "dispose-first", "dispose-third"]);
    }
    finally
    {
      await EndIfHostOpenAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      DisposeOrderLog.Reset();
    }
  }

  public static async Task EndWithoutBegin_Should_Throw()
  {
    TestRunner.ClearRegisteredSessionFixtures();
    try
    {
      InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
        async () => await TestRunner.EndTestSessionAsync());
      ex.Message.ShouldContain("no active host session");
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
    }
  }

  public static async Task SecondEnd_Should_Throw()
  {
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.BeginTestSession();
    await TestRunner.EndTestSessionAsync();
    try
    {
      InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
        async () => await TestRunner.EndTestSessionAsync());
      ex.Message.ShouldContain("no active host session");
    }
    finally
    {
      TestRunner.ClearRegisteredSessionFixtures();
    }
  }

  public static async Task ConcurrentRuns_Should_KeepTheFixtureUntilEveryRunEnds()
  {
    LeaseOverlap.Reset();
    OverlapSessionFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<OverlapSessionFixture>();
    try
    {
      Task<TestRunStats> runA = TestRunner.RunTestsAsync<OverlapRunA>(new SessionFixtureCollectingSink());
      Task<TestRunStats> runB = TestRunner.RunTestsAsync<OverlapRunB>(new SessionFixtureCollectingSink());
      try
      {
        await runA.WaitAsync(TimeSpan.FromSeconds(5));
      }
      finally
      {
        LeaseOverlap.AFinished.TrySetResult();
      }

      await runB.WaitAsync(TimeSpan.FromSeconds(5));
      runA.Result.Success.ShouldBeTrue();
      runB.Result.Success.ShouldBeTrue();
      OverlapSessionFixture.CreateCount.ShouldBe(1);
      OverlapSessionFixture.DisposeCount.ShouldBe(1);
      LeaseOverlap.SawDisposed.ShouldBe(0);
    }
    finally
    {
      LeaseOverlap.ReleaseA.TrySetResult();
      LeaseOverlap.AFinished.TrySetResult();
      TestRunner.ClearRegisteredSessionFixtures();
      OverlapSessionFixture.Reset();
      LeaseOverlap.Reset();
    }
  }

  public static async Task ConcurrentBeginEnd_Should_DisposeEachCreatedInstance()
  {
    HostStressFixture.Reset();
    TestRunner.ClearRegisteredSessionFixtures();
    TestRunner.RegisterSessionFixture<HostStressFixture>();
    try
    {
      Task[] workers = new Task[8];
      for (int index = 0; index < workers.Length; index++)
      {
        workers[index] = HostStressLoopAsync();
      }

      await Task.WhenAll(workers);
      HostStressFixture.CreateCount.ShouldBe(HostStressFixture.DisposeCount);
      HostStressFixture.CreateCount.ShouldBeGreaterThan(0);
    }
    finally
    {
      await EndIfHostOpenAsync();
      TestRunner.ClearRegisteredSessionFixtures();
      HostStressFixture.Reset();
    }
  }

  private static async Task HostStressLoopAsync()
  {
    for (int iteration = 0; iteration < 20; iteration++)
    {
      TestRunner.BeginTestSession();
      try
      {
        HostStressFixture fixture = await SessionFixture.GetAsync<HostStressFixture>();
        fixture.ShouldNotBeNull();
      }
      finally
      {
        await TestRunner.EndTestSessionAsync();
      }
    }
  }

  private static async Task EndIfHostOpenAsync()
  {
    try
    {
      await TestRunner.EndTestSessionAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains(
      "no active host session",
      StringComparison.Ordinal))
    {
    }
  }
}

public static class DisposeOrderLog
{
  private static readonly Lock Gate = new();
  private static readonly List<string> Events = [];

  public static void Reset()
  {
    lock (Gate)
    {
      Events.Clear();
    }
  }

  public static void Add(string name)
  {
    lock (Gate)
    {
      Events.Add(name);
    }
  }

  public static string[] Snapshot()
  {
    lock (Gate)
    {
      return [.. Events];
    }
  }
}

public sealed class FirstOrderFixture : IAsyncDisposable
{
  public static Task<FirstOrderFixture> CreateAsync()
  {
    return Task.FromResult(new FirstOrderFixture());
  }

  public ValueTask DisposeAsync()
  {
    DisposeOrderLog.Add("dispose-first");
    return ValueTask.CompletedTask;
  }
}

public sealed class SecondOrderFixture : IAsyncDisposable
{
  public static Task<SecondOrderFixture> CreateAsync()
  {
    return Task.FromResult(new SecondOrderFixture());
  }

  public ValueTask DisposeAsync()
  {
    DisposeOrderLog.Add("dispose-second");
    return ValueTask.CompletedTask;
  }
}

public sealed class ThirdOrderFixture : IAsyncDisposable
{
  public static Task<ThirdOrderFixture> CreateAsync()
  {
    return Task.FromResult(new ThirdOrderFixture());
  }

  public ValueTask DisposeAsync()
  {
    DisposeOrderLog.Add("dispose-third");
    return ValueTask.CompletedTask;
  }
}

public static class LeaseOverlap
{
  public static TaskCompletionSource AEntered { get; private set; } = NewSource();

  public static TaskCompletionSource ReleaseA { get; private set; } = NewSource();

  public static TaskCompletionSource AFinished { get; private set; } = NewSource();

  public static int SawDisposed;

  public static void Reset()
  {
    AEntered = NewSource();
    ReleaseA = NewSource();
    AFinished = NewSource();
    SawDisposed = 0;
  }

  private static TaskCompletionSource NewSource()
  {
    return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}

public sealed class OverlapSessionFixture : IAsyncDisposable
{
  public static int CreateCount;
  public static int DisposeCount;
  private volatile bool DisposedFlag;

  public bool Disposed => DisposedFlag;

  public static void Reset()
  {
    Interlocked.Exchange(ref CreateCount, 0);
    Interlocked.Exchange(ref DisposeCount, 0);
  }

  public static Task<OverlapSessionFixture> CreateAsync()
  {
    Interlocked.Increment(ref CreateCount);
    return Task.FromResult(new OverlapSessionFixture());
  }

  public ValueTask DisposeAsync()
  {
    DisposedFlag = true;
    Interlocked.Increment(ref DisposeCount);
    return ValueTask.CompletedTask;
  }
}

public sealed class HostStressFixture : IAsyncDisposable
{
  public static int CreateCount;
  public static int DisposeCount;

  public static void Reset()
  {
    Interlocked.Exchange(ref CreateCount, 0);
    Interlocked.Exchange(ref DisposeCount, 0);
  }

  public static Task<HostStressFixture> CreateAsync()
  {
    Interlocked.Increment(ref CreateCount);
    return Task.FromResult(new HostStressFixture());
  }

  public ValueTask DisposeAsync()
  {
    Interlocked.Increment(ref DisposeCount);
    return ValueTask.CompletedTask;
  }
}

public class OverlapRunA
{
  public static async Task OnlyTest()
  {
    _ = await SessionFixture.GetAsync<OverlapSessionFixture>();
    LeaseOverlap.AEntered.TrySetResult();
    await LeaseOverlap.ReleaseA.Task.WaitAsync(TimeSpan.FromSeconds(5));
  }
}

public class OverlapRunB
{
  public static async Task OnlyTest()
  {
    OverlapSessionFixture fixture = await SessionFixture.GetAsync<OverlapSessionFixture>();
    await LeaseOverlap.AEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
    LeaseOverlap.ReleaseA.TrySetResult();
    await LeaseOverlap.AFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));
    if (fixture.Disposed)
    {
      LeaseOverlap.SawDisposed = 1;
    }
  }
}
