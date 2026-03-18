namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// Basic tests to validate Microsoft.Testing.Platform integration.
/// Tests passing, failing, skipped, and timeout scenarios.
/// </summary>
[TestTag("MtpValidation")]
public class BasicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BasicTests>();

  #region Passing Tests

  /// <summary>
  /// Simple passing test - validates basic test execution.
  /// </summary>
  public static async Task PassingTest()
  {
    await Task.CompletedTask;
  }

  /// <summary>
  /// Another passing test with actual assertion.
  /// </summary>
  public static async Task AssertionPassingTest()
  {
    int result = 2 + 2;
    result.ShouldBe(4);
    await Task.CompletedTask;
  }

  /// <summary>
  /// Async test with small delay to verify async execution works.
  /// </summary>
  public static async Task AsyncWithDelayTest()
  {
    await Task.Delay(10);
  }

  #endregion

  #region Failing Tests

  /// <summary>
  /// Test that throws an exception - should report Failed state.
  /// </summary>
  public static async Task FailingWithExceptionTest()
  {
    await Task.CompletedTask;
    throw new InvalidOperationException("This test intentionally fails");
  }

  /// <summary>
  /// Test with failed assertion - should report Failed state.
  /// </summary>
  public static async Task FailingWithAssertionTest()
  {
    int result = 2 + 2;
    result.ShouldBe(5); // Will fail
    await Task.CompletedTask;
  }

  #endregion

  #region Skipped Tests

  /// <summary>
  /// Skipped test - should report Skipped state with reason.
  /// </summary>
  [Skip("Demonstrating skip functionality")]
  public static async Task SkippedTest()
  {
    await Task.CompletedTask;
    throw new InvalidOperationException("This should never execute");
  }

  /// <summary>
  /// Another skipped test with different reason.
  /// </summary>
  [Skip("Feature not yet implemented")]
  public static async Task SkippedFeatureTest()
  {
    await Task.CompletedTask;
  }

  #endregion

  #region Timeout Tests

  /// <summary>
  /// Test that exceeds timeout - should report Timeout state.
  /// </summary>
  [Timeout(50)]
  public static async Task TimeoutExceededTest()
  {
    await Task.Delay(5000); // Will timeout after 50ms
  }

  /// <summary>
  /// Test that completes within timeout - should pass.
  /// </summary>
  [Timeout(5000)]
  public static async Task TimeoutNotExceededTest()
  {
    await Task.Delay(10); // Completes well before 5000ms timeout
  }

  #endregion
}
