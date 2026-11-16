namespace TimeWarp.Jaribu;

#region Purpose
// The exception an instrumented member throws while prove mode is breaking it.
#endregion

/// <summary>
/// Thrown from an instrumented member when prove mode has marked that member broken.
/// </summary>
public sealed class ProbeBrokenException : Exception
{
  /// <summary>Creates an empty break. Serialization and the exception contract use this.</summary>
  public ProbeBrokenException()
    : base("Jaribu broke a production member.")
  {
    Token = string.Empty;
  }

  /// <summary>
  /// Creates the break for one instrumented member.
  /// </summary>
  /// <param name="token">Instrumented member token that was broken. Null is rejected.</param>
  public ProbeBrokenException(string token)
    : base($"Jaribu broke '{token}'.")
  {
    ArgumentNullException.ThrowIfNull(token);
    Token = token;
  }

  /// <summary>
  /// Creates the break with an inner exception.
  /// </summary>
  /// <param name="message">Exception message. Null is rejected.</param>
  /// <param name="innerException">The cause. Null is rejected.</param>
  public ProbeBrokenException(string message, Exception innerException)
    : base(message, innerException)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(innerException);
    Token = string.Empty;
  }

  /// <summary>Instrumented token of the member that threw.</summary>
  public string Token { get; }
}
