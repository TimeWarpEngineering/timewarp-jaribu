namespace TautologyAnalyzerFixture;

#region Purpose
// Methods that each match one static tautology rule.
#endregion

public static class Positive_
{
  public static async Task Empty_Should_Flag()
  {
  }

  public static async Task Completed_Should_FlagNoAssertion()
  {
    await Task.CompletedTask;
  }

  public static async Task Self_Should_Flag()
  {
    int value = 2;
    value.ShouldBe(value);
    await Task.CompletedTask;
  }

  public static async Task Literal_Should_Flag()
  {
    int value = 1;
    value.ShouldBe(1);
    await Task.CompletedTask;
  }
}
