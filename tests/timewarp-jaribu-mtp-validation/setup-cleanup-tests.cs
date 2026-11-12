namespace TimeWarp.Jaribu.MtpValidation;

#region Purpose
// Setup and CleanUp lifecycle cases for the MTP harness.
#endregion

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
    // Setup has run for this test and CleanUp for it has not.
    // How many earlier tests ran depends on order, so the check is only
    // that setup ran and that cleanup still trails setup by one.
    SetupCallCount.ShouldBeGreaterThan(0);
    CleanupCallCount.ShouldBe(SetupCallCount - 1);
    await Task.CompletedTask;
  }
}
