#region Purpose
// Session-fixture doubles and the nested classes that drive them.
#endregion

namespace TestRunner_;

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

public sealed class BlockedCreateSessionFixture : IAsyncDisposable
{
  public static TaskCompletionSource CreateStarted = new();
  public static TaskCompletionSource ReleaseCreate = new();
  public static int DisposeCount;

  public static void Reset()
  {
    CreateStarted = new TaskCompletionSource();
    ReleaseCreate = new TaskCompletionSource();
    DisposeCount = 0;
  }

  public static async Task<BlockedCreateSessionFixture> CreateAsync()
  {
    CreateStarted.SetResult();
    await ReleaseCreate.Task;
    return new BlockedCreateSessionFixture();
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

public class UsesBlockedCreateFromBody
{
  public static async Task OnlyTest()
  {
    _ = await SessionFixture.GetAsync<BlockedCreateSessionFixture>();
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

public class UsesFailingDisposeAndFailingBody
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
    throw new ArgumentException("body boom");
  }
}
