#!/usr/bin/dotnet --
#:project ../../../../source/timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests [Skip] attribute behavior and exception handling including runtime exceptions,
// wrapped TargetInvocationExceptions, and async exceptions after await.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (SkipExceptions_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.SkipExceptions_Given_.SkipAttribute_Should_ReportReason
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class SkipExceptions_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SkipExceptions_Given_>();

    /// <summary>
    /// Skipped test with reason - should skip and report reason.
    /// </summary>
    [Skip("WIP - Work in progress")]
    public static async Task SkipAttribute_Should_ReportReason()
    {
      WriteLine("SkipAttribute_Should_ReportReason: Should not run");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Runtime exception - ArgumentException.
    /// </summary>
    public static async Task RuntimeException_Should_Fail()
    {
      await Task.CompletedTask;
      throw new ArgumentException("Intentional runtime exception for SKIP-02");
    }

    /// <summary>
    /// TargetInvocationException - wrapped exception.
    /// </summary>
    public static async Task InvocationException_Should_UnwrapAndFail()
    {
      // This will be invoked via reflection, so throw to trigger TargetInvocationException
      await Task.CompletedTask;
      throw new InvalidOperationException("Inner exception for SKIP-03");
    }

    /// <summary>
    /// Async exception after await.
    /// </summary>
    public static async Task AsyncException_Should_FailAfterAwait()
    {
      await Task.Delay(1); // Await first
      throw new NotSupportedException("Async exception after await for SKIP-04");
    }

    /// <summary>
    /// Additional passing test to validate skipping doesn't affect others.
    /// </summary>
    public static async Task PassingMethod_Should_NotBeAffectedBySkips()
    {
      WriteLine("PassingMethod_Should_NotBeAffectedBySkips: Running successfully");
      await Task.CompletedTask;
    }
  }
}
