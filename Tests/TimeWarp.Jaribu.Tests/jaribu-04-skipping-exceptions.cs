#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Jaribu")]
public class SkipExceptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SkipExceptionTests>();

  /// <summary>
  /// Skipped test with reason - should skip and report reason.
  /// </summary>
  [Skip("WIP - Work in progress")]
  public static async Task SkippedTest()
  {
    WriteLine("SkippedTest: Should not run");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Runtime exception - ArgumentException.
  /// </summary>
  public static async Task ExceptionTest()
  {
    await Task.CompletedTask;
    throw new ArgumentException("Intentional runtime exception for SKIP-02");
  }

  /// <summary>
  /// TargetInvocationException - wrapped exception.
  /// </summary>
  public static async Task InvocationExceptionTest()
  {
    // This will be invoked via reflection, so throw to trigger TargetInvocationException
    await Task.CompletedTask;
    throw new InvalidOperationException("Inner exception for SKIP-03");
  }

  /// <summary>
  /// Async exception after await.
  /// </summary>
  public static async Task AsyncExceptionTest()
  {
    await Task.Delay(1); // Await first
    throw new NotSupportedException("Async exception after await for SKIP-04");
  }

  /// <summary>
  /// Additional passing test to validate skipping doesn't affect others.
  /// </summary>
  public static async Task PassingTest()
  {
    WriteLine("PassingTest: Running successfully");
    await Task.CompletedTask;
  }
}
