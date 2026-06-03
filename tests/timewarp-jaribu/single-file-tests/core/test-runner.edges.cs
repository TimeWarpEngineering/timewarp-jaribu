#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests edge cases including generic methods, mismatched parameter counts,
// multi-tag no-match, non-qualifying methods, ValueTask, timeout, and cancellation.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Edges_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Edges_Given_.GenericMethod_Should_HandleReflection
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class Edges_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Edges_Given_>();

    /// <summary>
    /// Generic test method - reflection should handle generics.
    /// </summary>
    public static async Task GenericMethod_Should_HandleReflection<T>()
    {
      WriteLine($"GenericMethod_Should_HandleReflection: Running with type {typeof(T).Name}");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Additional edge for [Input] with mismatched count (0 for 3 params).
    /// Should fail gracefully.
    /// </summary>
    [Input]
    public static async Task MismatchedParamCount_Should_FailGracefully(string p1, int p2, bool p3)
    {
      WriteLine($"MismatchedParamCount_Should_FailGracefully: {p1}, {p2}, {p3} - Unexpected if mismatched");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Method with multiple tags - already in TagTests, but additional with no match.
    /// </summary>
    [TestTag("no-match1")]
    [TestTag("no-match2")]
    public static async Task MultiTagNoMatch_Should_SkipWhenUnmatched()
    {
      WriteLine("MultiTagNoMatch_Should_SkipWhenUnmatched: Should skip if filter doesn't match any");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Class with 0 qualifying tests - summary "0/0", exit=0.
    /// Include non-qualifying methods only.
    /// </summary>
    // No qualifying methods here - this class tests zero tests scenario

    public static void NonQualifyingMethod_Should_BeSkipped()
    {
      // Sync void: skipped
    }

    private static async Task PrivateMethod_Should_BeSkipped()
    {
      // Private: skipped
      await Task.CompletedTask;
    }

    /// <summary>
    /// Additional edge: Method with ValueTask (future support).
    /// Currently not run due to strict Task check.
    /// </summary>
    public static ValueTask ValueTask_Should_NotRunUntilSupported()
    {
      WriteLine("ValueTask_Should_NotRunUntilSupported: Should not run until supported");
      return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Edge: Hanging test simulation (no timeout implemented).
    /// </summary>
    [Timeout(5000)]
    public static async Task HangingTest_Should_Timeout()
    {
      // Infinite loop or long delay - manual timeout check
      await Task.Delay(Timeout.Infinite); // Simulates hang
    }

    /// <summary>
    /// Edge: Test with CancellationToken (future).
    /// </summary>
    public static async Task CancellationToken_Should_BeSupported(CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested(); // If supported
      await Task.CompletedTask;
    }
  }
}
