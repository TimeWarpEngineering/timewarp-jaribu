namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// Tests that validate Setup and CleanUp lifecycle methods.
/// </summary>
[TestTag("Lifecycle")]
public class SetupCleanupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SetupCleanupTests>();

  private static int SetupCallCount;
  private static int CleanupCallCount;
  private static bool SetupWasCalled;

  /// <summary>
  /// Setup is called before each test.
  /// </summary>
  public static async Task Setup()
  {
    SetupCallCount++;
    SetupWasCalled = true;
    await Task.CompletedTask;
  }

  /// <summary>
  /// CleanUp is called after each test.
  /// </summary>
  public static async Task CleanUp()
  {
    CleanupCallCount++;
    await Task.CompletedTask;
  }

  /// <summary>
  /// Verifies Setup was called before this test.
  /// </summary>
  public static async Task SetupWasCalledTest()
  {
    SetupWasCalled.ShouldBeTrue();
    await Task.CompletedTask;
  }

  /// <summary>
  /// Another test to verify Setup is called for each test.
  /// </summary>
  public static async Task SetupCalledMultipleTimesTest()
  {
    // After this test runs, SetupCallCount should be at least 2
    // (once for SetupWasCalledTest, once for this test)
    SetupCallCount.ShouldBeGreaterThan(0);
    // Inside any test body, every prior test's CleanUp has run and ours is pending,
    // so cleanups always trail setups by exactly one — regardless of test order.
    CleanupCallCount.ShouldBe(SetupCallCount - 1);
    await Task.CompletedTask;
  }
}
