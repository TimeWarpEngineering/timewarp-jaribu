namespace TautologyFixture;

#region Purpose
// Production methods the detector covers and breaks. Add calls Scale.
#endregion

/// <summary>Production methods the detector covers and breaks.</summary>
public static class TautologySubject
{
  /// <summary>Adds <paramref name="right"/> after scaling it.</summary>
  public static int Add(int left, int right) => left + Scale(right);

  /// <summary>Returns <paramref name="value"/> unchanged. Add calls this.</summary>
  public static int Scale(int value) => value;

  /// <summary>Adds every value. The parameter type is a closed generic.</summary>
  public static int Sum(IEnumerable<int> values)
  {
    ArgumentNullException.ThrowIfNull(values);
    int total = 0;
    foreach (int value in values)
    {
      total = Add(total, value);
    }

    return total;
  }
}

/// <summary>Auto-property the detector claims with <c>nameof</c>.</summary>
public sealed class Reading
{
  /// <summary>Value stored by the auto-property.</summary>
  public int Value { get; set; }
}

/// <summary>Closed generic whose property is claimed through <c>typeof(Slot&lt;int&gt;)</c>.</summary>
public sealed class Slot<T>
  where T : struct
{
  /// <summary>Item stored by the auto-property.</summary>
  public T Item { get; set; }
}
