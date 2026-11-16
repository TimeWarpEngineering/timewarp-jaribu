namespace TautologyFixture;

#region Purpose
// Production methods the detector covers and breaks. Add calls Scale.
#endregion

/// <summary>Production methods the detector covers and breaks.</summary>
public static class TautologySubject
{
  /// <summary>Adds <paramref name="right"/> after scaling it.</summary>
  public static int Add(int left, int right) => left + Scale(right);

  /// <summary>Returns <paramref name="value"/> unchanged. This is the more specific callee.</summary>
  public static int Scale(int value) => value;
}
