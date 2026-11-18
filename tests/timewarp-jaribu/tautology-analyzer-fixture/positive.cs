namespace TautologyAnalyzerFixture;

#region Purpose
// Methods that each match one static tautology rule.
#endregion

// An xUnit-style assertion type. The analyzer matches assertion APIs by type, not by method name.
public static class Assert
{
  public static void Equal<T>(T expected, T actual)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
      throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
  }
}

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

  public static async Task Contains_Should_FlagNoAssertion()
  {
    List<int> values = [1];
    values.Contains(1);
    await Task.CompletedTask;
  }

  public static async Task AssertEqual_Should_Flag()
  {
    int expected = 2;
    Assert.Equal(expected, expected);
    await Task.CompletedTask;
  }
}
