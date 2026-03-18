#!/usr/bin/dotnet --
#:project ../../../../source/timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests for test runner reporting output and cleanup lifecycle hooks
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Reporting_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Reporting_Given_.MixedResults_Should_Pass
//
// ReportTests has intentionally failing tests and zero-test classes
// to exercise summary output and CleanUp invocation.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Output")]
  public class Reporting_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Reporting_Given_>();

    /// <summary>
    /// 2 passing, 1 failing - verify summary "2/3 passed".
    /// </summary>
    public static async Task MixedResults_Should_CountPassingTest1()
    {
      await Task.CompletedTask;
    }

    public static async Task MixedResults_Should_CountPassingTest2()
    {
      await Task.CompletedTask;
    }

    public static async Task MixedResults_Should_CountFailingTest()
    {
      await Task.CompletedTask;
      throw new InvalidOperationException("Intentional fail for REPORT-01");
    }

    /// <summary>
    /// Test for filtered summary - combine with tag filter.
    /// This untagged method should run in filtered context.
    /// </summary>
    public static async Task FilteredContext_Should_RunPassingTest()
    {
      await Task.CompletedTask;
    }

    /// <summary>
    /// CleanUp method - should invoke after tests, log message.
    /// </summary>
    public static async Task CleanUp()
    {
      WriteLine("CleanUp invoked");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Test to highlight counter accumulation (if not reset).
    /// Run multiple times to see if counters accumulate.
    /// </summary>
    public static async Task MultipleRuns_Should_NotAccumulateCounters()
    {
      await Task.CompletedTask;
    }

    /// <summary>
    /// Class with 0 tests - summary "0/0", exit=0.
    /// No methods here.
    /// </summary>
  }

  [TestTag("Output")]
  public class ZeroTests_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ZeroTests_Given_>();

    // No test methods - should TotalTests=0, exit=0
    public static async Task CleanUp()
    {
      WriteLine("CleanUp invoked");
      await Task.CompletedTask;
    }
  }
}
