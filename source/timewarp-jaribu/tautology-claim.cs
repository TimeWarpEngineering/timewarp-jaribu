namespace TimeWarp.Jaribu;

#region Purpose
// Turns a [Proves] pair, or a covered token, into the prefix Enter matches.
#endregion

/// <summary>
/// One member claim. The prefix matches every overload the instrumenter emitted.
/// </summary>
/// <param name="Display">Type and method, without the parameter list.</param>
/// <param name="Prefix"><c>Assembly|Namespace.Type.Method(</c></param>
internal readonly record struct TautologyClaim(string Display, string Prefix)
{
  /// <summary>Builds the prefix <see cref="ProvesAttribute"/> names.</summary>
  public static TautologyClaim From(Type type, string methodName)
  {
    ArgumentNullException.ThrowIfNull(type);
    ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
    string assembly = type.Assembly.GetName().Name ?? string.Empty;
    string typeName = type.FullName ?? type.Name;
    return new TautologyClaim(
      typeName + "." + methodName,
      assembly + "|" + typeName + "." + methodName + "(");
  }

  /// <summary>
  /// Builds a claim from an instrumented token.
  /// Returns null when the token has no assembly bar or parameter list.
  /// </summary>
  public static TautologyClaim? FromToken(string token)
  {
    ArgumentNullException.ThrowIfNull(token);
    int bar = token.IndexOf('|', StringComparison.Ordinal);
    int paren = token.IndexOf('(', StringComparison.Ordinal);
    if (bar <= 0 || paren <= bar + 1)
    {
      return null;
    }

    return new TautologyClaim(token[(bar + 1)..paren], token[..(paren + 1)]);
  }
}
