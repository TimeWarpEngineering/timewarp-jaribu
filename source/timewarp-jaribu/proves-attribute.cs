namespace TimeWarp.Jaribu;

#region Purpose
// Names the production member a test claims to prove.
#endregion

/// <summary>
/// Declares that a test proves <paramref name="methodName"/> on <paramref name="type"/>.
/// A normal run ignores the claim. Prove mode breaks that member and requires the test to fail.
/// </summary>
/// <param name="type">Declaring type of the production member. Null is rejected.</param>
/// <param name="methodName">
/// Member name, as <c>nameof</c> returns it. A property or event names its accessors.
/// A closed generic type is read as its generic type definition. Null and blank are rejected.
/// Every overload of that name is one claim.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProvesAttribute : Attribute
{
  /// <summary>Declaring type of the claimed member.</summary>
  public Type Type { get; }

  /// <summary>Claimed member name.</summary>
  public string MethodName { get; }

  /// <summary>
  /// Creates a claim.
  /// </summary>
  /// <param name="type">Declaring type of the production member.</param>
  /// <param name="methodName">Member name. Overloads share one claim.</param>
  /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="methodName"/> is null.</exception>
  /// <exception cref="ArgumentException"><paramref name="methodName"/> is blank.</exception>
  public ProvesAttribute(Type type, string methodName)
  {
    ArgumentNullException.ThrowIfNull(type);
    ArgumentNullException.ThrowIfNull(methodName);
    if (string.IsNullOrWhiteSpace(methodName))
    {
      throw new ArgumentException("A [Proves] method name cannot be blank.", nameof(methodName));
    }

    Type = type;
    MethodName = methodName;
  }
}
