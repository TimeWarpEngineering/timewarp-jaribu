namespace TimeWarp.Jaribu;

#region Purpose
// Opts a smoke or compile-only test out of strict tautology failures.
#endregion

/// <summary>
/// Marks a test as a known tautology. Prove mode still lists it. Strict mode does not fail the run for it.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class KnownTautologyAttribute : Attribute
{
  /// <summary>Why this test is allowed to prove nothing.</summary>
  public string Reason { get; }

  /// <summary>
  /// Creates the opt-out.
  /// </summary>
  /// <param name="reason">Why the test is a known tautology. Null is rejected. Empty and whitespace are kept.</param>
  /// <exception cref="ArgumentNullException"><paramref name="reason"/> is null.</exception>
  public KnownTautologyAttribute(string reason)
  {
    ArgumentNullException.ThrowIfNull(reason);
    Reason = reason;
  }
}
