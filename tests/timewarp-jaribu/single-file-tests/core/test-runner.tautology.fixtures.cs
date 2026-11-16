namespace Tautology_;

using TautologyFixture;

#region Purpose
// Production subjects the detector runs under coverage and prove mode.
#endregion

public class Explicit_Given_
{
  [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
  public static async Task Add_Should_ReturnTheSum()
  {
    TautologySubject.Add(2, 3).ShouldBe(5);
    await Task.CompletedTask;
  }
}

public class Inferred_Given_
{
  public static async Task Add_Should_ImplyTheDirectCall()
  {
    TautologySubject.Add(2, 3).ShouldBe(5);
    await Task.CompletedTask;
  }
}

public class TrailingGetter_Given_
{
  public static async Task Add_Should_IgnoreTheGetter()
  {
    int sum = TautologySubject.Add(2, 3);
    Reading reading = new() { Value = sum };
    reading.Value.ShouldBe(5);
    await Task.CompletedTask;
  }
}

public class Generic_Given_
{
  public static async Task Sum_Should_AddTheValues()
  {
    TautologySubject.Sum([1, 2]).ShouldBe(3);
    await Task.CompletedTask;
  }
}

public class Property_Given_
{
  [Proves(typeof(Reading), nameof(Reading.Value))]
  public static async Task Value_Should_RoundTrip()
  {
    Reading reading = new() { Value = 4 };
    reading.Value.ShouldBe(4);
    await Task.CompletedTask;
  }
}

public class ClosedGeneric_Given_
{
  [Proves(typeof(Slot<int>), nameof(Slot<int>.Item))]
  public static async Task Item_Should_RoundTrip()
  {
    Slot<int> slot = new() { Item = 4 };
    slot.Item.ShouldBe(4);
    await Task.CompletedTask;
  }
}

public class Unknown_Given_
{
  [Proves(typeof(TautologySubject), "MissingMember")]
  public static async Task Add_Should_StillPass()
  {
    TautologySubject.Add(1, 1).ShouldBe(2);
    await Task.CompletedTask;
  }
}

public class Nested_Given_
{
  public static async Task Scale_Should_RunAfterTheInnerRunner()
  {
    await RunTestsAsync<Constant_Given_>(new TautologyCollectingSink());
    TautologySubject.Scale(1).ShouldBe(1);
    await Task.CompletedTask;
  }
}

public class Discarded_Given_
{
  [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
  public static async Task Add_Should_DiscardTheResult()
  {
    try
    {
      _ = TautologySubject.Add(2, 3);
    }
    catch (ProbeBrokenException broken)
    {
      _ = broken.Token;
    }

    true.ShouldBeTrue();
    await Task.CompletedTask;
  }
}

public class Constant_Given_
{
  public static async Task Value_Should_EqualTheSameLiteral()
  {
    1.ShouldBe(1);
    await Task.CompletedTask;
  }
}

public class Known_Given_
{
  [KnownTautology("compile-only smoke")]
  public static async Task Smoke_Should_StayListed()
  {
    await Task.CompletedTask;
  }
}

public class Rank_Given_
{
  [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
  public static async Task Keep_Should_ReturnTheSum()
  {
    TautologySubject.Add(2, 3).ShouldBe(5);
    await Task.CompletedTask;
  }

  [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
  public static async Task Discard_Should_IgnoreTheThrow()
  {
    try
    {
      _ = TautologySubject.Add(2, 3);
    }
    catch (ProbeBrokenException broken)
    {
      _ = broken.Token;
    }

    true.ShouldBeTrue();
    await Task.CompletedTask;
  }
}
