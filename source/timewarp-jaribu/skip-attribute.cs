namespace TimeWarp.Jaribu;

#region Purpose
// Marks a test method skipped and keeps the caller's reason.
#endregion

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
  /// <param name="reason">Why the test is skipped. Null is rejected. Empty and whitespace are kept.</param>
  /// <exception cref="ArgumentNullException"><paramref name="reason"/> is null.</exception>
  public SkipAttribute(string reason)
  {
    ArgumentNullException.ThrowIfNull(reason);
    Reason = reason;
  }
}
