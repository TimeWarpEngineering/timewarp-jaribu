#region Purpose
// Scenario fixtures for SetupOnce and CleanUpOnce. These classes are not registered.
#endregion

namespace TestRunner_;

// --- Fixtures (not registered) ---

public class OnceOnlyFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task TestA()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  public static async Task TestB()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class CleanupAfterFailFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task PassingTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  public static async Task FailingTest()
  {
    BodyCount++;
    await Task.CompletedTask;
    throw new InvalidOperationException("intentional body failure");
  }
}

public class AllSkipFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  [Skip("lazy setup check")]
  public static async Task SkippedA()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  [Skip("lazy setup check")]
  public static async Task SkippedB()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class AllTagFilteredFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  [TestTag("alpha")]
  public static async Task TaggedA()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  [TestTag("beta")]
  public static async Task TaggedB()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class SetupOnceFailureFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
    throw new InvalidOperationException("SetupOnce boom");
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task TestA()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  public static async Task TestB()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class CleanUpOnceFailureFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
    throw new InvalidOperationException("CleanUpOnce boom");
  }

  public static async Task PassingTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class BadSignatureVoidFixture
{
  public static int BodyCount;
  public static int CleanUpOnceCount;

  public static void Reset()
  {
    BodyCount = 0;
    CleanUpOnceCount = 0;
  }

  public static void SetupOnce()
  {
    // Intentionally wrong signature (void, not Task)
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task RealTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class BadSignaturePrivateFixture
{
  public static int BodyCount;
  public static int CleanUpOnceCount;

  public static void Reset()
  {
    BodyCount = 0;
    CleanUpOnceCount = 0;
  }

  private static async Task SetupOnce()
  {
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task RealTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class DiscoveryExclusionFixture
{
  public static async Task SetupOnce() => await Task.CompletedTask;
  public static async Task CleanUpOnce() => await Task.CompletedTask;
  public static async Task Setup() => await Task.CompletedTask;
  public static async Task CleanUp() => await Task.CompletedTask;
  public static async Task RealTest() => await Task.CompletedTask;
}

public class CoexistFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int SetupCount;
  public static int CleanUpCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    SetupCount = 0;
    CleanUpCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task Setup()
  {
    SetupCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    CleanUpCount++;
    await Task.CompletedTask;
  }

  public static async Task TestA()
  {
    BodyCount++;
    await Task.CompletedTask;
  }

  public static async Task TestB()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class ParameterizedOnceFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  [Input("a")]
  [Input("b")]
  public static async Task ParamTest(string value)
  {
    BodyCount++;
    value.ShouldNotBeNullOrEmpty();
    await Task.CompletedTask;
  }
}

public class BypassFixture
{
  public static int SetupOnceCount;
  public static int CleanUpOnceCount;
  public static int SetupCount;
  public static int CleanUpCount;
  public static int BodyCount;

  public static void Reset()
  {
    SetupOnceCount = 0;
    CleanUpOnceCount = 0;
    SetupCount = 0;
    CleanUpCount = 0;
    BodyCount = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    CleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task Setup()
  {
    SetupCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    CleanUpCount++;
    await Task.CompletedTask;
  }

  public static async Task OnlyTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}
