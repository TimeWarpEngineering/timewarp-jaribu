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
// [Skip] and a method-level tag miss stay one method-level node.
// EnumerateReportedCases is that list for discovery, the skip and tag short-circuits,
// and bad once-hook fan-out. ExpandInputs is only the rows that actually execute,
// after those short-circuits.
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
  /// Active tag filter. A method whose own tags exist and none match contributes one method-level node.
  /// </param>
  public static IReadOnlyList<ReportedTestCase> EnumerateReportedCases(
    Type testClass,
    MethodInfo method,
    string? filterTag)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    if (IsMethodTagMismatch(method, filterTag) || method.GetCustomAttribute<SkipAttribute>() is not null)
    {
      return [MethodCase(testClass, method)];
    }

    return ExpandInputs(testClass, method);
  }

  /// <summary>
  /// Lists every reported case for <paramref name="testClass"/> in discovery order.
  /// </summary>
  public static IEnumerable<ReportedTestCase> DiscoverTestCases(Type testClass, string? filterTag = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    foreach (MethodInfo method in TestRunner.DiscoverTests(testClass))
    {
      foreach (ReportedTestCase testCase in EnumerateReportedCases(testClass, method, filterTag))
      {
        yield return testCase;
      }
    }
  }

  private static ReportedTestCase MethodCase(Type testClass, MethodInfo method)
  {
    return new ReportedTestCase(
      method,
      InputOrdinal: null,
      MethodUid(testClass, method),
      method.Name,
      Parameters: null);
  }

  private static bool IsMethodTagMismatch(MethodInfo method, string? filterTag)
  {
    if (filterTag is null)
    {
      return false;
    }

    TestTagAttribute[] tags = [.. method.GetCustomAttributes<TestTagAttribute>()];
    return tags.Length > 0 &&
      !tags.Any(tag => tag.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase));
  }

  private static string FormatArgumentList(IReadOnlyList<object?> parameters)
  {
    return string.Join(", ", parameters.Select(static value => value?.ToString() ?? "null"));
  }
}
