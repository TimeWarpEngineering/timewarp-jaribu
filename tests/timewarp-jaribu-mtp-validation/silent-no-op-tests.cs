namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// One real test beside a near-miss, so a default MTP run must say the near-miss was skipped.
/// </summary>
[TestTag("SignatureWarning")]
public class SignatureWarningTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SignatureWarningTests>();

  public static async Task Real()
  {
    await Task.CompletedTask;
  }

  public static Task<int> ReturnsTaskOfInt() => Task.FromResult(1);
}

/// <summary>
/// Registered class with nothing to run. A full or class-filtered run fails unless --allow-empty is set.
/// </summary>
[TestTag("AllowEmptyProbe")]
public class AllowEmptyProbeTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AllowEmptyProbeTests>();
}
