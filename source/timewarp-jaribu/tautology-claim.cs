namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Turns a [Proves] pair, or a covered token, into the prefixes Enter matches.
#endregion

/// <summary>
/// One member claim. Each prefix matches every overload the instrumenter emitted for that member.
/// </summary>
/// <param name="Display">Type and member, without the parameter list.</param>
/// <param name="Prefixes"><c>Assembly|Namespace.Type.Member(</c> for each accessor or the method.</param>
internal readonly record struct TautologyClaim(string Display, IReadOnlyList<string> Prefixes)
{
  private const BindingFlags Flags =
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

  /// <summary>
  /// Builds the prefixes <see cref="ProvesAttribute"/> names.
  /// A property or event expands to its accessors. A closed generic type uses the generic type definition.
  /// </summary>
  /// <returns>False when <paramref name="methodName"/> matches no method, property, or event.</returns>
  public static bool TryFrom(Type type, string methodName, out TautologyClaim claim, out string error)
  {
    ArgumentNullException.ThrowIfNull(type);
    ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
    Type declared = OpenType(type);
    List<string> prefixes = [];
    AddMethods(declared, methodName, prefixes);
    if (prefixes.Count == 0)
    {
      AddProperties(declared, methodName, prefixes);
    }

    if (prefixes.Count == 0)
    {
      AddEvents(declared, methodName, prefixes);
    }

    if (prefixes.Count == 0)
    {
      claim = default;
      string typeName = declared.FullName ?? declared.Name;
      error = "'" + typeName + "." + methodName + "' matches no method, property, or event.";
      return false;
    }

    string displayType = declared.FullName ?? declared.Name;
    claim = new TautologyClaim(displayType + "." + methodName, prefixes);
    error = string.Empty;
    return true;
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

    return new TautologyClaim(token[(bar + 1)..paren], [token[..(paren + 1)]]);
  }

  private static void AddMethods(Type type, string methodName, List<string> prefixes)
  {
    foreach (MethodInfo method in type.GetMethods(Flags))
    {
      if (method.Name != methodName)
      {
        continue;
      }

      AddPrefix(prefixes, method.DeclaringType ?? type, method.Name);
    }
  }

  private static void AddProperties(Type type, string methodName, List<string> prefixes)
  {
    foreach (PropertyInfo property in type.GetProperties(Flags))
    {
      if (property.Name != methodName)
      {
        continue;
      }

      MethodInfo? getter = property.GetGetMethod(nonPublic: true);
      MethodInfo? setter = property.GetSetMethod(nonPublic: true);
      if (getter is not null)
      {
        AddPrefix(prefixes, getter.DeclaringType ?? type, getter.Name);
      }

      if (setter is not null)
      {
        AddPrefix(prefixes, setter.DeclaringType ?? type, setter.Name);
      }
    }
  }

  private static void AddEvents(Type type, string methodName, List<string> prefixes)
  {
    foreach (EventInfo eventInfo in type.GetEvents(Flags))
    {
      if (eventInfo.Name != methodName)
      {
        continue;
      }

      MethodInfo? add = eventInfo.GetAddMethod(nonPublic: true);
      MethodInfo? remove = eventInfo.GetRemoveMethod(nonPublic: true);
      if (add is not null)
      {
        AddPrefix(prefixes, add.DeclaringType ?? type, add.Name);
      }

      if (remove is not null)
      {
        AddPrefix(prefixes, remove.DeclaringType ?? type, remove.Name);
      }
    }
  }

  private static void AddPrefix(List<string> prefixes, Type type, string memberName)
  {
    string prefix = PrefixFor(type, memberName);
    if (!prefixes.Contains(prefix, StringComparer.Ordinal))
    {
      prefixes.Add(prefix);
    }
  }

  private static string PrefixFor(Type type, string memberName)
  {
    Type declared = OpenType(type);
    string assembly = declared.Assembly.GetName().Name ?? string.Empty;
    string typeName = declared.FullName ?? declared.Name;
    return assembly + "|" + typeName + "." + memberName + "(";
  }

  private static Type OpenType(Type type)
  {
    if (type.IsGenericType && !type.IsGenericTypeDefinition)
    {
      return type.GetGenericTypeDefinition();
    }

    return type;
  }
}
