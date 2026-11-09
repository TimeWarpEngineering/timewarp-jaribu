namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;
using System.Text;

#region Purpose
// Stable identity for each reported test node, including one node per [Input] row.
#endregion

#region Design
// The ordinal is the metadata-table order of [Input]. The C# compiler writes
// that table in source order. A method with no [Input] keeps {Type.FullName}.{Method}.
// Each row is {Type.FullName}.{Method}[ordinal]. When discovery finds another
// test method of the same name, both uids append the parameter type list.
// Argument text uses the invariant culture, quotes strings, and lists array
// elements. A direct CaseUid call with arguments and no ordinal uses that text.
// [Input(null)] is one null argument. [Skip] and a tag miss stay one method-level
// node. EnumerateReportedCases is that list for discovery, those short-circuits,
// and bad once-hook fan-out. ExpandInputs is only the rows that execute.
// DiscoverTestCases reads JARIBU_FILTER_TAG when filterTag is null.
// A method whose Jaribu attribute constructor throws is one method-level case.
// The run reports that case as Error, including when the class tag misses.
#endregion

/// <summary>
/// Builds the node ids and the case list shared by <see cref="TestRunner"/> and MTP discovery.
/// </summary>
public static class TestCaseIdentity
{
  /// <summary>
  /// Returns the method-level id used when a method is one node.
  /// An overloaded discovered test appends its parameter type list.
  /// </summary>
  public static string MethodUid(Type testClass, MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    string stem = $"{testClass.FullName}.{method.Name}";
    if (!HasDiscoveredOverload(testClass, method))
    {
      return stem;
    }

    return string.Concat(stem, "(", FormatParameterSignature(method), ")");
  }

  /// <summary>
  /// Returns the id of one <see cref="InputAttribute"/> row.
  /// </summary>
  public static string InputUid(Type testClass, MethodInfo method, int inputOrdinal)
  {
    string ordinal = inputOrdinal.ToString(CultureInfo.InvariantCulture);
    return $"{MethodUid(testClass, method)}[{ordinal}]";
  }

  /// <summary>
  /// Returns the id <see cref="TestRunner.RunSingleTestAsync"/> stamps on a node.
  /// An explicit ordinal wins. Otherwise non-empty arguments are formatted so a direct
  /// call still does not reuse the method id.
  /// </summary>
  public static string CaseUid(
    Type testClass,
    MethodInfo method,
    int? inputOrdinal,
    IReadOnlyList<object?>? parameters)
  {
    if (inputOrdinal is int ordinal)
    {
      return InputUid(testClass, method, ordinal);
    }

    if (parameters is { Count: > 0 })
    {
      return $"{MethodUid(testClass, method)}({FormatArgumentList(parameters)})";
    }

    return MethodUid(testClass, method);
  }

  /// <summary>
  /// Returns the display name discovery and execution share for one method or row.
  /// Overloaded methods include the parameter type list before any argument text.
  /// </summary>
  public static string FormatReportedName(
    Type testClass,
    MethodInfo method,
    IReadOnlyList<object?>? parameters)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    return NodeDisplayName(testClass, method, parameters);
  }

  /// <summary>
  /// Formats a display name. Rows with no arguments keep the method name.
  /// Arguments use the invariant culture. Strings are quoted. Arrays list their elements.
  /// </summary>
  public static string FormatDisplayName(string methodName, IReadOnlyList<object?>? parameters)
  {
    ArgumentException.ThrowIfNullOrEmpty(methodName);
    if (parameters is null || parameters.Count == 0)
    {
      return methodName;
    }

    return $"{methodName}({FormatArgumentList(parameters)})";
  }

  /// <summary>
  /// Expands every <see cref="InputAttribute"/> row. A method with none yields one method-level case.
  /// </summary>
  public static IReadOnlyList<ReportedTestCase> ExpandInputs(Type testClass, MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    InputAttribute[] inputs = InputRowOrder.Ordered(method);
    if (inputs.Length == 0)
    {
      return [MethodCase(testClass, method)];
    }

    ReportedTestCase[] cases = new ReportedTestCase[inputs.Length];
    for (int ordinal = 0; ordinal < inputs.Length; ordinal++)
    {
      object?[] parameters = inputs[ordinal].Parameters;
      IReadOnlyList<object?>? listed = parameters.Length > 0 ? parameters : null;
      cases[ordinal] = new ReportedTestCase(
        method,
        ordinal,
        InputUid(testClass, method, ordinal),
        FormatReportedName(testClass, method, listed),
        listed);
    }

    return cases;
  }

  /// <summary>
  /// Returns the nodes a discovery listing and a normal run both emit.
  /// </summary>
  /// <param name="testClass">Class that declares <paramref name="method"/>.</param>
  /// <param name="method">Candidate test method.</param>
  /// <param name="filterTag">
  /// Active tag filter. A class or method whose own tags exist and none match contributes one method-level node.
  /// </param>
  public static IReadOnlyList<ReportedTestCase> EnumerateReportedCases(
    Type testClass,
    MethodInfo method,
    string? filterTag)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    if (JaribuAttributeRead.TryRejectMethod(method, out _))
    {
      return [MethodCase(testClass, method)];
    }

    if (TestFilter.TagSkipReason(testClass, method, filterTag) is not null ||
        method.GetCustomAttribute<SkipAttribute>() is not null)
    {
      return [MethodCase(testClass, method)];
    }

    return ExpandInputs(testClass, method);
  }

  /// <summary>
  /// Lists every reported case for <paramref name="testClass"/> in discovery order.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <param name="filterTag">
  /// Tag filter. Null reads <c>JARIBU_FILTER_TAG</c>. A blank value is not a filter.
  /// A class or method whose tags exist and none match contributes one method-level node.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Non-matches are omitted.
  /// </param>
  /// <param name="methodPredicate">Optional additional selection predicate. Non-matches are omitted.</param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A node is omitted when it does not match.
  /// </param>
  public static IEnumerable<ReportedTestCase> DiscoverTestCases(
    Type testClass,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    return TestFilter.SelectReportedCases(
      testClass,
      TestFilter.ResolveTag(filterTag),
      methodNameContains,
      methodPredicate,
      casePredicate);
  }

  internal static ReportedTestCase MethodCase(Type testClass, MethodInfo method)
  {
    return new ReportedTestCase(
      method,
      InputOrdinal: null,
      MethodUid(testClass, method),
      FormatReportedName(testClass, method, parameters: null),
      Parameters: null);
  }

  private static string NodeDisplayName(
    Type testClass,
    MethodInfo method,
    IReadOnlyList<object?>? parameters)
  {
    string name = method.Name;
    if (HasDiscoveredOverload(testClass, method))
    {
      name = string.Concat(name, "(", FormatParameterSignature(method), ")");
    }

    return FormatDisplayName(name, parameters);
  }

  private static bool HasDiscoveredOverload(Type testClass, MethodInfo method)
  {
    int count = 0;
    foreach (MethodInfo candidate in TestRunner.DiscoverTests(testClass))
    {
      if (!string.Equals(candidate.Name, method.Name, StringComparison.Ordinal))
      {
        continue;
      }

      count++;
      if (count > 1)
      {
        return true;
      }
    }

    return false;
  }

  private static string FormatParameterSignature(MethodInfo method)
  {
    ParameterInfo[] parameters = method.GetParameters();
    string[] parts = new string[parameters.Length];
    for (int index = 0; index < parameters.Length; index++)
    {
      parts[index] = FormatParameterType(parameters[index].ParameterType);
    }

    return string.Join(", ", parts);
  }

  private static string FormatParameterType(Type type)
  {
    if (type.IsByRef || type.IsPointer)
    {
      string marker = type.IsByRef ? "&" : "*";
      return string.Concat(FormatParameterType(type.GetElementType()!), marker);
    }

    if (type.IsArray)
    {
      int rank = type.GetArrayRank();
      string commas = rank <= 1 ? string.Empty : new string(',', rank - 1);
      return string.Concat(FormatParameterType(type.GetElementType()!), "[", commas, "]");
    }

    if (type.IsGenericType)
    {
      string definition = type.GetGenericTypeDefinition().FullName ?? type.Name;
      int tick = definition.IndexOf('`', StringComparison.Ordinal);
      if (tick >= 0)
      {
        definition = definition[..tick];
      }

      string arguments = string.Join(", ", type.GetGenericArguments().Select(FormatParameterType));
      return string.Concat(definition, "<", arguments, ">");
    }

    return type.FullName ?? type.Name;
  }

  private static string FormatArgumentList(IReadOnlyList<object?> parameters)
  {
    string[] parts = new string[parameters.Count];
    for (int index = 0; index < parameters.Count; index++)
    {
      parts[index] = FormatArgument(parameters[index]);
    }

    return string.Join(", ", parts);
  }

  private static string FormatArgument(object? value)
  {
    switch (value)
    {
      case null:
        return "null";
      case string text:
        return Quote(text);
      case Array array:
        return FormatArray(array);
      case IFormattable formattable:
        return formattable.ToString(null, CultureInfo.InvariantCulture);
      default:
        return value.ToString() ?? string.Empty;
    }
  }

  private static string FormatArray(Array array)
  {
    string[] parts = new string[array.Length];
    int index = 0;
    foreach (object? item in array)
    {
      parts[index] = FormatArgument(item);
      index++;
    }

    return string.Concat("[", string.Join(", ", parts), "]");
  }

  private static string Quote(string text)
  {
    StringBuilder builder = new(text.Length + 2);
    builder.Append('"');
    foreach (char character in text)
    {
      switch (character)
      {
        case '\\':
          builder.Append("\\\\");
          break;
        case '"':
          builder.Append("\\\"");
          break;
        case '\r':
          builder.Append("\\r");
          break;
        case '\n':
          builder.Append("\\n");
          break;
        case '\t':
          builder.Append("\\t");
          break;
        default:
          builder.Append(character);
          break;
      }
    }

    builder.Append('"');
    return builder.ToString();
  }
}
