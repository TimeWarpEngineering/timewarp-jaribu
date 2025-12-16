#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

#if !JARIBU_MULTI
RegisterTests<DiscoveryTests>();
return await RunAllTests();
#endif

[TestTag("Jaribu")]
public class DiscoveryTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DiscoveryTests>();

  private static int SetupCount;
  private static int CleanUpCount;

  /// <summary>
  /// Basic test method execution - Simple passing test.
  /// </summary>
  public static async Task BasicTest()
  {
    // No-op: passes by default
    await Task.CompletedTask;
  }

  /// <summary>
  /// Non-qualifying methods - These should be skipped.
  /// </summary>
  public static void NonAsyncVoidTest()
  {
    // Sync void: should skip
  }

  private static async Task PrivateAsyncTest()
  {
    // Private: should skip
    await Task.CompletedTask;
  }

  public static async Task Setup()
  {
    // Named Setup: invoked before each test
    SetupCount++;
    WriteLine($"Setup invoked (count: {SetupCount}) - preparing test environment");
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    // Named CleanUp: invoked after each test (async)
    CleanUpCount++;
    WriteLine($"CleanUp invoked (count: {CleanUpCount})");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Failing test for multi-test validation.
  /// </summary>
  public static async Task FailingTest()
  {
    await Task.CompletedTask;
    throw new ArgumentException("Intentional failure");
  }

  /// <summary>
  /// Another passing test.
  /// </summary>
  public static async Task PassingTest2()
  {
    await Task.CompletedTask;
  }

  /// <summary>
  /// Async test with await.
  /// </summary>
  public static async Task AsyncAwaitTest()
  {
    await Task.Delay(1); // Simulates async work
  }

  /// <summary>
  /// ValueTask test (future enhancement - currently not supported).
  /// Uncomment when ValueTask support added.
  /// </summary>
  // public static ValueTask ValueTaskTest()
  // {
  //     return ValueTask.CompletedTask;
  // }
}
