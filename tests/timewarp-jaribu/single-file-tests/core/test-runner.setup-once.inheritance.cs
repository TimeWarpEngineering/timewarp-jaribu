#region Purpose
// Base and derived once-hook fixtures used by the inheritance meta-tests.
#endregion

namespace TestRunner_;

public class OnceHookBase
{
  public static int BaseSetupOnceCount;
  public static int BaseCleanUpOnceCount;

  public static void ResetBase()
  {
    BaseSetupOnceCount = 0;
    BaseCleanUpOnceCount = 0;
  }

  public static async Task SetupOnce()
  {
    BaseSetupOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task CleanUpOnce()
  {
    BaseCleanUpOnceCount++;
    await Task.CompletedTask;
  }
}

public class DerivedPreferFixture : OnceHookBase
{
  public static int DerivedSetupOnceCount;
  public static int DerivedCleanUpOnceCount;
  public static int BodyCount;

  public static void Reset()
  {
    ResetBase();
    DerivedSetupOnceCount = 0;
    DerivedCleanUpOnceCount = 0;
    BodyCount = 0;
  }

  public static new async Task SetupOnce()
  {
    DerivedSetupOnceCount++;
    await Task.CompletedTask;
  }

  public static new async Task CleanUpOnce()
  {
    DerivedCleanUpOnceCount++;
    await Task.CompletedTask;
  }

  public static async Task OnlyTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class BaseOnlyHookFixture : OnceHookBase
{
  public static int BodyCount;

  public static void Reset()
  {
    ResetBase();
    BodyCount = 0;
  }

  public static async Task OnlyTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}

public class OverloadOnceFixture
{
  public static int BodyCount;

  public static void Reset()
  {
    BodyCount = 0;
  }

  // Two methods named SetupOnce on the same type — fail-fast resolution error.
  public static async Task SetupOnce()
  {
    await Task.CompletedTask;
  }

  public static async Task SetupOnce(int unused)
  {
    _ = unused;
    await Task.CompletedTask;
  }

  public static async Task OnlyTest()
  {
    BodyCount++;
    await Task.CompletedTask;
  }
}
