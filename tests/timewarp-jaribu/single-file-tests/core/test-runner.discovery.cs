#!/usr/bin/dotnet --
#:project ../../../../source/timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests that the test runner discovers, executes, and filters methods correctly
// including Setup/CleanUp lifecycle, async methods, and non-qualifying method exclusion.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Discovery_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Discovery_Given_.BasicMethod_Should_Pass
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class Discovery_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Discovery_Given_>();

    private static int SetupCount;
    private static int CleanUpCount;

    /// <summary>
    /// Basic test method execution - Simple passing test.
    /// </summary>
    public static async Task BasicMethod_Should_Pass()
    {
      // No-op: passes by default
      await Task.CompletedTask;
    }

    /// <summary>
    /// Non-qualifying methods - These should be skipped.
    /// </summary>
    public static void NonAsyncVoidMethod_Should_BeSkipped()
    {
      // Sync void: should skip
    }

    private static async Task PrivateAsyncMethod_Should_BeSkipped()
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
    public static async Task IntentionalFailure_Should_Fail()
    {
      await Task.CompletedTask;
      throw new ArgumentException("Intentional failure");
    }

    /// <summary>
    /// Another passing test.
    /// </summary>
    public static async Task SecondPassingMethod_Should_Pass()
    {
      await Task.CompletedTask;
    }

    /// <summary>
    /// Async test with await.
    /// </summary>
    public static async Task AsyncAwait_Should_Complete()
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
}
