namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// Edge case tests for Microsoft.Testing.Platform integration.
/// </summary>
[TestTag("EdgeCases")]
public class EdgeCaseTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EdgeCaseTests>();

  /// <summary>
  /// Test with null reference exception.
  /// </summary>
  public static async Task NullReferenceExceptionTest()
  {
    await Task.CompletedTask;
    string? nullString = null;
    _ = nullString!.Length; // Will throw NullReferenceException
  }

  /// <summary>
  /// Test with argument exception.
  /// </summary>
  public static async Task ArgumentExceptionTest()
  {
    await Task.CompletedTask;
    throw new ArgumentException("Invalid argument provided", "testParam");
  }

  /// <summary>
  /// Test that passes after some work.
  /// </summary>
  public static async Task WorkThenPassTest()
  {
    int sum = 0;
    for (int i = 0; i < 100; i++)
    {
      sum += i;
    }

    sum.ShouldBe(4950); // Sum of 0..99
    await Task.CompletedTask;
  }

  /// <summary>
  /// Test with multiple assertions - all pass.
  /// </summary>
  public static async Task MultipleAssertionsPassTest()
  {
    int a = 5;
    int b = 10;

    a.ShouldBeLessThan(b);
    b.ShouldBeGreaterThan(a);
    (a + b).ShouldBe(15);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test with very short timeout that should still pass.
  /// </summary>
  [Timeout(1000)]
  public static async Task QuickOperationWithTimeoutTest()
  {
    // Immediate completion - should pass well within timeout
    await Task.CompletedTask;
  }
}
