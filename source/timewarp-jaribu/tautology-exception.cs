namespace TimeWarp.Jaribu;

#region Purpose
// The failure strict tautology mode attaches to a test that proved nothing.
#endregion

/// <summary>
/// Reported when strict mode turns an unproven, uncovered, or unclaimed test into a failure.
/// </summary>
public sealed class TautologyException : Exception
{
  /// <summary>Creates an empty failure. Serialization and the exception contract use this.</summary>
  public TautologyException()
    : base("The test proved nothing.")
  {
  }

  /// <summary>
  /// Creates the failure.
  /// </summary>
  /// <param name="message">The one-line tautology reason. Null is rejected.</param>
  /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
  public TautologyException(string message)
    : base(message)
  {
    ArgumentNullException.ThrowIfNull(message);
  }

  /// <summary>
  /// Creates the failure with an inner exception.
  /// </summary>
  /// <param name="message">The one-line tautology reason. Null is rejected.</param>
  /// <param name="innerException">The cause. Null is rejected.</param>
  public TautologyException(string message, Exception innerException)
    : base(message, innerException)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(innerException);
  }
}
