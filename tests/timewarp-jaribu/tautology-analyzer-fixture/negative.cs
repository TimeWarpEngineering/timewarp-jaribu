namespace TautologyAnalyzerFixture;

#region Purpose
// Methods the static tautology rules must leave alone.
#endregion

public static class NegativeSubject
{
  public static int Compute(int value) => value;
}

public static class Negative_
{
  public static async Task Compute_Should_NotFlag()
  {
    int value = NegativeSubject.Compute(2);
    value.ShouldBe(2);
    await Task.CompletedTask;
  }

  public static async Task DifferentSymbols_Should_NotFlag()
  {
    int left = 1;
    int right = 1;
    left.ShouldBe(right);
    await Task.CompletedTask;
  }

  [KnownTautology("compile-only smoke")]
  public static async Task Known_Should_NotFlag()
  {
    await Task.CompletedTask;
  }
}
