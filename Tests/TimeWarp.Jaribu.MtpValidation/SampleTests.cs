namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// Sample tests to validate Microsoft.Testing.Platform integration.
/// </summary>
[TestTag("MtpValidation")]
public class SampleTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SampleTests>();

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
}
