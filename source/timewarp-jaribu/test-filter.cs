namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// One filter for class tags, method tags, and method name, shared by discovery and the class run.
#endregion

#region Design
// A blank tag is not a filter. JARIBU_FILTER_TAG is read only when the caller
// passes null, and a whitespace value is ignored. A non-blank value is trimmed.
// Tag comparison is ordinal ignore-case and includes inherited attributes.
// A member with no tags matches any tag filter. A member with tags matches
// when any tag equals the filter.
// A class tag miss and a method tag miss both contribute one method-level
// Skipped node, with the same message. They do not expand [Input] rows.
// Method-name filters and a caller's case predicate omit non-matches and do
// not emit Skipped nodes. The MTP adapter does not call this type. It passes
// a raw tag or null into the public discovery and run methods, and it
// compares uid lists itself.
// [Skip] stays a Skipped node. Its reason is the attribute text.
// SkippedNode is the factory for those short-circuit nodes.
// RunSingleTestAsync keeps its own node because that call measures duration
// and keeps the caller's parameters.
#endregion

/// <summary>
/// Selects reported cases and builds the Skipped node for a tag miss or <see cref="SkipAttribute"/>.
/// </summary>
internal static class TestFilter
{
  /// <summary>Environment variable that supplies the tag filter when the caller passes null.</summary>
  internal const string EnvironmentVariableName = "JARIBU_FILTER_TAG";

  /// <summary>
  /// Returns null when <paramref name="value"/> is null or whitespace. Otherwise returns the trimmed value.
  /// </summary>
  public static string? Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return value.Trim();
  }

  /// <summary>
  /// Uses <paramref name="explicitTag"/> when the caller passed one, including a blank that means no filter.
  /// A null argument reads <see cref="EnvironmentVariableName"/>.
  /// </summary>
  public static string? ResolveTag(string? explicitTag)
  {
    if (explicitTag is not null)
    {
      return Normalize(explicitTag);
    }

    return ReadEnvironmentTag();
  }

  /// <summary>
  /// Reads <see cref="EnvironmentVariableName"/> and applies <see cref="Normalize"/>.
  /// </summary>
  public static string? ReadEnvironmentTag()
  {
    return Normalize(Environment.GetEnvironmentVariable(EnvironmentVariableName));
  }

  /// <summary>
  /// Returns whether the class passes <paramref name="filterTag"/>.
  /// No tags, or a null filter, passes. Tags that exist must include the filter.
  /// </summary>
  public static bool ClassTagMatches(Type testClass, string? filterTag)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    return TagMatches(testClass, Normalize(filterTag));
  }

  /// <summary>
  /// Returns the shared tag-miss message, or null when the class and the method both pass.
  /// </summary>
  public static string? TagSkipReason(Type testClass, MethodInfo method, string? filterTag)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    string? tag = Normalize(filterTag);
    if (tag is null)
    {
      return null;
    }

    if (!TagMatches(testClass, tag) || !TagMatches(method, tag))
    {
      return TagMissMessage(tag);
    }

    return null;
  }

  /// <summary>Message stamped on a class or method tag miss.</summary>
  public static string TagMissMessage(string filterTag)
  {
    ArgumentException.ThrowIfNullOrEmpty(filterTag);
    return $"No matching tag '{filterTag}'";
  }

  /// <summary>
  /// Returns whether the method name contains <paramref name="methodNameContains"/>.
  /// A null filter matches every method.
  /// </summary>
  public static bool MethodNameMatches(MethodInfo method, string? methodNameContains)
  {
    ArgumentNullException.ThrowIfNull(method);
    if (methodNameContains is null)
    {
      return true;
    }

    return method.Name.Contains(methodNameContains, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Lists the cases discovery and a normal run both report.
  /// <paramref name="filterTag"/> is already resolved: this method does not read the environment.
  /// </summary>
  public static IEnumerable<ReportedTestCase> SelectReportedCases(
    Type testClass,
    string? filterTag,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    filterTag = Normalize(filterTag);

    foreach (MethodInfo method in TestRunner.DiscoverTests(testClass))
    {
      if (!MethodNameMatches(method, methodNameContains))
      {
        continue;
      }

      if (methodPredicate is not null && !methodPredicate(method))
      {
        continue;
      }

      foreach (ReportedTestCase testCase in TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag))
      {
        if (casePredicate is not null && !casePredicate(testCase))
        {
          continue;
        }

        yield return testCase;
      }
    }
  }

  /// <summary>
  /// Builds the Skipped node for a tag miss or <see cref="SkipAttribute"/>.
  /// The node is the method, not an <see cref="InputAttribute"/> row.
  /// </summary>
  public static TestNodeInfo SkippedNode(ReportedTestCase testCase, string? message)
  {
    DateTimeOffset stamp = DateTimeOffset.UtcNow;
    return new TestNodeInfo(
      testCase.Uid,
      testCase.DisplayName,
      TestNodeState.Skipped,
      TimeSpan.Zero,
      Exception: null,
      message,
      Parameters: null,
      Method: testCase.Method,
      StartTime: stamp,
      EndTime: stamp);
  }

  private static bool TagMatches(MemberInfo member, string? filterTag)
  {
    if (filterTag is null)
    {
      return true;
    }

    bool any = false;
    foreach (TestTagAttribute tag in member.GetCustomAttributes<TestTagAttribute>(inherit: true))
    {
      any = true;
      if (tag.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return !any;
  }
}
