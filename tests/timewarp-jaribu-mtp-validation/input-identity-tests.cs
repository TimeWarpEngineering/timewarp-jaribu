namespace TimeWarp.Jaribu.MtpValidation;

/// <summary>
/// One failing row followed by a passing row. Discovery and the run must both report two nodes.
/// </summary>
[TestTag("InputIdentity")]
public class InputIdentityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InputIdentityTests>();

  [Input("fail")]
  [Input("pass")]
  public static async Task FailThenPass(string value)
  {
    if (value == "fail")
    {
      throw new InvalidOperationException("first input fails");
    }

    await Task.CompletedTask;
  }
}
