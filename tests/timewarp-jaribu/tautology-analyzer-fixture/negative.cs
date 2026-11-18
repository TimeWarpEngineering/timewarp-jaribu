namespace TautologyAnalyzerFixture;

#region Purpose
// Methods the static tautology rules must leave alone.
#endregion

public static class NegativeSubject
{
  public static int Compute(int value) => value;

  // Production methods whose names look like assertions. They are not assertions.
  public static bool ShouldRefuse(string cwd, string gitRoot, string? claimPath) =>
    string.Equals(cwd, gitRoot, StringComparison.Ordinal) && claimPath is not null;

  public static bool Same(int left, int right) => left == right;
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

  public static async Task Reassigned_Should_NotFlag()
  {
    int value = -42;
    value = NegativeSubject.Compute(-42);
    value.ShouldBe(-42);
    await Task.CompletedTask;
  }

  public static async Task Incremented_Should_NotFlag()
  {
    int value = 17;
    value++;
    value.ShouldBe(17);
    await Task.CompletedTask;
  }

  public static async Task Throw_Should_NotFlag()
  {
    if (NegativeSubject.Compute(1) != 1)
    {
      throw new InvalidOperationException("missed");
    }
  }

  [TimeWarp.Jaribu.KnownTautology("qualified form")]
  public static async Task QualifiedKnown_Should_NotFlag()
  {
    await Task.CompletedTask;
  }

  public static async Task Generic_Should_NotFlag<T>()
  {
    await Task.CompletedTask;
  }

  public static async Task LoopAssigned_Should_NotFlag()
  {
    bool saw = false;
    int[] results = [-1, 1];
    foreach (int result in results)
    {
      if (result < 0)
      {
        saw = true;
      }
    }

    saw.ShouldBe(true);
    await Task.CompletedTask;
  }

  public static async Task ShouldNamedProduction_Should_NotFlag()
  {
    string claim = "/claim";
    string root = "/root";
    NegativeSubject.ShouldRefuse(claim, claim, claim).ShouldBeTrue();
    NegativeSubject.ShouldRefuse(root, root, null).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task AssertNamedProduction_Should_NotFlag()
  {
    int value = NegativeSubject.Compute(3);
    NegativeSubject.Same(value, value).ShouldBeTrue();
    await Task.CompletedTask;
  }
}
