namespace TimeWarp.Jaribu.TestingPlatform;

using System.Collections;
using System.Globalization;

#region Purpose
// One formatting path for the type name and parameter text MTP nodes and filters share.
#endregion

#region Design
// MtpSink publishes jaribu.parameter.N, and MtpTreeFilter matches those values.
// A second copy of the type name or the parameter text would let a filter miss
// the row the IDE lists. TestTag stays on the filter bag; this helper does not
// publish it. The key prefix itself stays on MtpTreeFilter so the sink reads
// the filter's constant.
// Strings stay unquoted so an existing filter value such as fail still matches.
// Arrays and other sequences print their elements. The CLR type name is not
// a value a filter can select.
#endregion

/// <summary>
/// Shared metadata type name and parameter text for MTP node publication and tree filters.
/// </summary>
internal static class MtpNodeMetadata
{
  /// <summary>
  /// Metadata type name without the namespace. Nested types keep <c>+</c>. Generic arity keeps the backtick.
  /// </summary>
  public static string MetadataTypeName(Type type)
  {
    ArgumentNullException.ThrowIfNull(type);
    string fullName = type.FullName ?? type.Name;
    string namespaceName = type.Namespace ?? string.Empty;
    if (namespaceName.Length == 0)
    {
      return fullName;
    }

    string prefix = string.Concat(namespaceName, ".");
    if (fullName.StartsWith(prefix, StringComparison.Ordinal))
    {
      return fullName[prefix.Length..];
    }

    return fullName;
  }

  /// <summary>
  /// Text form of one <see cref="InputAttribute"/> argument, matching the value a tree filter compares.
  /// </summary>
  public static string FormatParameter(object? value)
  {
    return value switch
    {
      null => "null",
      string text => text,
      bool flag => flag ? "true" : "false",
      IEnumerable sequence => FormatSequence(sequence),
      IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
      _ => value.ToString() ?? string.Empty
    };
  }

  private static string FormatSequence(IEnumerable values)
  {
    List<string> parts = [];
    foreach (object? item in values)
    {
      parts.Add(FormatParameter(item));
    }

    return "[" + string.Join(",", parts) + "]";
  }
}
