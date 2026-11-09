namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;

#region Purpose
// Stable identity for each reported test node, including one node per [Input] row.
#endregion

#region Design
// The ordinal is the metadata order of [Input], which is source declaration order.
// A method with no [Input] keeps {Type.FullName}.{Method}. Each row is
// {Type.FullName}.{Method}[ordinal], so two rows with the same arguments stay distinct
// and the id does not depend on culture-sensitive formatting.
// [Skip] and a tag miss stay one method-level node. A class whose tags exist
// and none match uses that same node for every discovered method, so discovery
// and the run list the same ids. EnumerateReportedCases is that list for
// discovery, the skip and tag short-circuits, and bad once-hook fan-out.
// ExpandInputs is only the rows that actually execute, after those short-circuits.
// DiscoverTestCases reads JARIBU_FILTER_TAG when filterTag is null, matching RunTestsAsync.
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
  /// </summary>
  public static string MethodUid(Type testClass, MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    return $"{testClass.FullName}.{method.Name}";
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
  /// Formats a display name. Rows with no arguments keep the method name.
  /// </summary>
  public static string FormatDisplayName(string methodName, IReadOnlyList<object?>? parameters)
  {
    ArgumentException.ThrowIfNullOrEmpty(methodName);
    if (parameters is not { Count: > 0 })
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

    InputAttribute[] inputs = [.. method.GetCustomAttributes<InputAttribute>()];
    if (inputs.Length == 0)
    {
      return [MethodCase(testClass, method)];
    }

    ReportedTestCase[] cases = new ReportedTestCase[inputs.Length];
    for (int ordinal = 0; ordinal < inputs.Length; ordinal++)
    {
      object?[] parameters = inputs[ordinal].Parameters ?? [];
      IReadOnlyList<object?>? listed = parameters.Length > 0 ? parameters : null;
      cases[ordinal] = new ReportedTestCase(
        method,
        ordinal,
        InputUid(testClass, method, ordinal),
        FormatDisplayName(method.Name, parameters),
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
      method.Name,
      Parameters: null);
  }

  private static string FormatArgumentList(IReadOnlyList<object?> parameters)
  {
    return string.Join(", ", parameters.Select(static value => value?.ToString() ?? "null"));
  }
}
