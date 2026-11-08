namespace TimeWarp.Jaribu.TestingPlatform;

using System.Globalization;
using System.Reflection;

#region Purpose
// Turns one reported case into the path and property bag TreeNodeFilter expects.
#endregion

#region Design
// The path is /Assembly/Namespace/Type/Method. Assembly is the simple name.
// Type keeps nested names with '+' and drops the namespace, matching the
// method-identifier property. A slash inside a segment is percent-encoded so
// it cannot open another level. '+' stays literal because filters write it that way.
// The bag carries TestTag for each class and method tag. TestTag exists only
// in the filter bag; MtpSink does not publish it. jaribu.parameter.N uses the
// same key prefix and the same MtpNodeMetadata text MtpSink publishes.
// Property filters read only TestMetadataProperty. NopFilter and the uid list
// stay on this switch so an unrecognized filter fails the request instead of
// running every case.
#endregion

/// <summary>
/// Matches one reported case against the MTP execution filter.
/// </summary>
internal static class MtpTreeFilter
{
  /// <summary>Metadata key for a <see cref="TestTagAttribute"/> value.</summary>
  public const string TestTagKey = "TestTag";

  /// <summary>Metadata key prefix for one <see cref="InputAttribute"/> argument.</summary>
  public const string ParameterKeyPrefix = "jaribu.parameter.";

  /// <summary>
  /// Returns whether <paramref name="filter"/> selects <paramref name="testCase"/>.
  /// </summary>
  public static bool Matches(Type testClass, ReportedTestCase testCase, ITestExecutionFilter filter)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(filter);

#pragma warning disable TPEXP // TreeNodeFilter is experimental
    return filter switch
    {
      NopFilter => true,
      TestNodeUidListFilter uidFilter =>
        TestFilter.UidMatches(testCase.Uid, uidFilter.TestNodeUids.Select(static uid => uid.Value)),
      TreeNodeFilter treeFilter =>
        treeFilter.MatchesFilter(BuildPath(testClass, testCase.Method), BuildProperties(testClass, testCase)),
      _ => throw new NotSupportedException(
        $"Unsupported MTP filter type '{filter.GetType().FullName}'.")
    };
#pragma warning restore TPEXP
  }

  /// <summary>
  /// Builds <c>/Assembly/Namespace/Type/Method</c> for <paramref name="method"/>.
  /// </summary>
  public static string BuildPath(Type testClass, MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    string assemblyName = testClass.Assembly.GetName().Name ?? string.Empty;
    string namespaceName = testClass.Namespace ?? string.Empty;
    return string.Concat(
      "/",
      EncodeSegment(assemblyName),
      "/",
      EncodeSegment(namespaceName),
      "/",
      EncodeSegment(MtpNodeMetadata.MetadataTypeName(testClass)),
      "/",
      EncodeSegment(method.Name));
  }

  /// <summary>
  /// Builds the property bag property filters evaluate for <paramref name="testCase"/>.
  /// </summary>
  public static PropertyBag BuildProperties(Type testClass, ReportedTestCase testCase)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    PropertyBag bag = new();
    AddTags(bag, testClass);
    AddTags(bag, testCase.Method);

    IReadOnlyList<object?>? parameters = testCase.Parameters;
    if (parameters is not { Count: > 0 })
    {
      return bag;
    }

    for (int index = 0; index < parameters.Count; index++)
    {
      string key = string.Concat(
        ParameterKeyPrefix,
        index.ToString(CultureInfo.InvariantCulture));
      bag.Add(new TestMetadataProperty(key, MtpNodeMetadata.FormatParameter(parameters[index])));
    }

    return bag;
  }

  private static void AddTags(PropertyBag bag, MemberInfo member)
  {
    foreach (TestTagAttribute tag in member.GetCustomAttributes<TestTagAttribute>())
    {
      if (string.IsNullOrEmpty(tag.Tag))
      {
        continue;
      }

      bag.Add(new TestMetadataProperty(TestTagKey, tag.Tag));
    }
  }

  private static string EncodeSegment(string segment)
  {
    if (segment.IndexOf('/', StringComparison.Ordinal) < 0)
    {
      return segment;
    }

    return segment.Replace("/", "%2F", StringComparison.Ordinal);
  }
}
