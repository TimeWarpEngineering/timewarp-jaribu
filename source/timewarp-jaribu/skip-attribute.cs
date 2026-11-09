namespace TimeWarp.Jaribu;

/// <summary>
/// Marks a test method to be skipped during test execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipAttribute : Attribute
{
  /// <summary>
  /// Gets the reason this test is being skipped.
  /// </summary>
  public string Reason { get; }

  /// <summary>
  /// Creates a skip mark.
  /// </summary>
  /// <param name="reason">Why the test is skipped. Null, empty, and whitespace are rejected.</param>
  /// <exception cref="ArgumentNullException"><paramref name="reason"/> is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="reason"/> is empty or whitespace.</exception>
  public SkipAttribute(string reason)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(reason);
    Reason = reason;
  }
}
